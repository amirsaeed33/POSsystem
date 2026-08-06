using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.MultiTenancy;
using Abp.Runtime.Security;
using Abp.UI;
using SmartPos.Authentication.EmailLogin;
using SmartPos.Authentication.External;
using SmartPos.Authentication.JwtBearer;
using SmartPos.Authorization;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Emailing;
using SmartPos.Models.TokenAuth;
using SmartPos.MultiTenancy;
using SmartPos.Net.Emailing;
using Abp.Domain.Repositories;

namespace SmartPos.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TokenAuthController : SmartPosControllerBase
    {
        private readonly LogInManager _logInManager;
        private readonly ITenantCache _tenantCache;
        private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
        private readonly TokenAuthConfiguration _configuration;
        private readonly IExternalAuthConfiguration _externalAuthConfiguration;
        private readonly IExternalAuthManager _externalAuthManager;
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly UserClaimsPrincipalFactory _claimsPrincipalFactory;
        private readonly EmailLoginCodeStore _emailLoginCodeStore;
        private readonly ISmtpMailSender _smtpMailSender;
        private readonly IRepository<EmailTemplate> _emailTemplateRepository;

        public TokenAuthController(
            LogInManager logInManager,
            ITenantCache tenantCache,
            AbpLoginResultTypeHelper abpLoginResultTypeHelper,
            TokenAuthConfiguration configuration,
            IExternalAuthConfiguration externalAuthConfiguration,
            IExternalAuthManager externalAuthManager,
            UserManager userManager,
            RoleManager roleManager,
            UserClaimsPrincipalFactory claimsPrincipalFactory,
            EmailLoginCodeStore emailLoginCodeStore,
            ISmtpMailSender smtpMailSender,
            IRepository<EmailTemplate> emailTemplateRepository)
        {
            _logInManager = logInManager;
            _tenantCache = tenantCache;
            _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
            _configuration = configuration;
            _externalAuthConfiguration = externalAuthConfiguration;
            _externalAuthManager = externalAuthManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _emailLoginCodeStore = emailLoginCodeStore;
            _smtpMailSender = smtpMailSender;
            _emailTemplateRepository = emailTemplateRepository;
        }

        [HttpPost]
        public async Task<AuthenticateResultModel> Authenticate([FromBody] AuthenticateModel model)
        {
            // Resolve tenant from the user account — do not require the client to select tenancy.
            var tenancyName = await ResolveTenancyNameForLoginAsync(model.UserNameOrEmailAddress);
            var loginResult = await GetLoginResultAsync(
                model.UserNameOrEmailAddress,
                model.Password,
                tenancyName
            );

            var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));

            return new AuthenticateResultModel
            {
                AccessToken = accessToken,
                EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                UserId = loginResult.User.Id,
                TenantId = loginResult.User.TenantId,
                TenancyName = tenancyName ?? loginResult.Tenant?.TenancyName
            };
        }

        [HttpPost]
        public async Task<SendEmailLoginCodeResultModel> SendEmailLoginCode(
            [FromBody] SendEmailLoginCodeModel model)
        {
            EnsureEmailLoginEnabled();

            var email = (model?.EmailAddress ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new UserFriendlyException("Email address is required.");
            }

            var user = await FindUniqueActiveUserByLoginAsync(email);
            if (user == null)
            {
                throw new UserFriendlyException(
                    "No active account was found for that email address.");
            }

            var code = _emailLoginCodeStore.CreateCode(user.TenantId, email);
            var expirationMinutes = _emailLoginCodeStore.ExpirationMinutes;

            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Code"] = code,
                ["ExpirationMinutes"] = expirationMinutes.ToString(),
                ["UserName"] = user.UserName ?? string.Empty,
                ["Name"] = string.IsNullOrWhiteSpace(user.Name)
                    ? (user.UserName ?? "there")
                    : $"{user.Name} {user.Surname}".Trim(),
                ["Email"] = user.EmailAddress ?? email,
                ["AppName"] = "SmartPos"
            };

            var (subject, bodyHtml) = await ResolveEmailLoginTemplateAsync(placeholders);
            await _smtpMailSender.SendAsync(user.EmailAddress, subject, bodyHtml, isBodyHtml: true);

            return new SendEmailLoginCodeResultModel
            {
                ExpirationMinutes = expirationMinutes,
                ResendCooldownSeconds = _emailLoginCodeStore.ResendCooldownSeconds
            };
        }

        private async Task<(string Subject, string BodyHtml)> ResolveEmailLoginTemplateAsync(
            IReadOnlyDictionary<string, string> placeholders)
        {
            var template = await _emailTemplateRepository.FirstOrDefaultAsync(
                x => x.Code == EmailTemplateCodes.EmailLoginCode && x.IsActive);

            var subjectTemplate = template?.Subject ?? "Your {{AppName}} sign-in code";
            var bodyTemplate = !string.IsNullOrWhiteSpace(template?.BodyHtml)
                ? template.BodyHtml
                : EmailTemplateDefaults.EmailLoginCodeBodyHtml();

            return (
                EmailTemplateRenderer.Render(subjectTemplate, placeholders),
                EmailTemplateRenderer.Render(bodyTemplate, placeholders)
            );
        }

        [HttpPost]
        public async Task<AuthenticateResultModel> AuthenticateWithEmailCode(
            [FromBody] AuthenticateWithEmailCodeModel model)
        {
            EnsureEmailLoginEnabled();

            var email = (model?.EmailAddress ?? string.Empty).Trim();
            var code = (model?.Code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                throw new UserFriendlyException("Email address and code are required.");
            }

            var user = await FindUniqueActiveUserByLoginAsync(email);
            if (user == null)
            {
                throw new UserFriendlyException(
                    "No active account was found for that email address.");
            }

            _emailLoginCodeStore.VerifyAndConsume(user.TenantId, email, code);

            ClaimsIdentity identity;
            using (CurrentUnitOfWork.SetTenantId(user.TenantId))
            {
                var principal = await _claimsPrincipalFactory.CreateAsync(user);
                identity = principal.Identity as ClaimsIdentity;
            }

            if (identity == null)
            {
                throw new UserFriendlyException("Could not create login identity.");
            }

            var accessToken = CreateAccessToken(CreateJwtClaims(identity));
            var tenancyName = GetTenancyNameByTenantId(user.TenantId);

            return new AuthenticateResultModel
            {
                AccessToken = accessToken,
                EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                UserId = user.Id,
                TenantId = user.TenantId,
                TenancyName = tenancyName
            };
        }

        [HttpGet]
        public List<ExternalLoginProviderInfoModel> GetExternalAuthenticationProviders()
        {
            return _externalAuthConfiguration.Providers
                .Select(p => new ExternalLoginProviderInfoModel
                {
                    Name = p.Name,
                    ClientId = p.ClientId
                })
                .ToList();
        }

        [HttpPost]
        public async Task<ExternalAuthenticateResultModel> ExternalAuthenticate(
            [FromBody] ExternalAuthenticateModel model)
        {
            var externalUser = await GetExternalUserInfo(model);
            var tenancyName = await ResolveTenancyNameForLoginAsync(externalUser.EmailAddress);

            var loginResult = await _logInManager.LoginAsync(
                new UserLoginInfo(model.AuthProvider, externalUser.ProviderKey, model.AuthProvider),
                tenancyName
            );

            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                {
                    var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));
                    return new ExternalAuthenticateResultModel
                    {
                        AccessToken = accessToken,
                        EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                        ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                        UserId = loginResult.User.Id,
                        TenantId = loginResult.User.TenantId,
                        TenancyName = tenancyName ?? loginResult.Tenant?.TenancyName
                    };
                }
                case AbpLoginResultType.UnknownExternalLogin:
                    throw new UserFriendlyException(
                        "No account is linked to this external login. Contact your administrator.");
                default:
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                        loginResult.Result,
                        externalUser.ProviderKey,
                        tenancyName
                    );
            }
        }

        private void EnsureEmailLoginEnabled()
        {
            if (!_emailLoginCodeStore.IsEnabled)
            {
                throw new UserFriendlyException("Email sign-in is disabled.");
            }
        }

        private async Task<User> RegisterExternalUserAsync(ExternalAuthUserInfo externalUser)
        {
            var existingUser = await _userManager.FindByEmailAsync(externalUser.EmailAddress);
            if (existingUser != null)
            {
                CheckErrors(await _userManager.AddLoginAsync(
                    existingUser,
                    new UserLoginInfo(externalUser.Provider, externalUser.ProviderKey, externalUser.Provider)
                ));
                return existingUser;
            }

            var user = new User
            {
                TenantId = AbpSession.TenantId,
                EmailAddress = externalUser.EmailAddress,
                Name = string.IsNullOrWhiteSpace(externalUser.Name) ? externalUser.EmailAddress : externalUser.Name,
                Surname = string.IsNullOrWhiteSpace(externalUser.Surname) ? "-" : externalUser.Surname,
                IsActive = true,
                IsEmailConfirmed = true,
                UserName = await EnsureUniqueUserNameAsync(externalUser.EmailAddress),
                Roles = new List<UserRole>()
            };

            user.SetNormalizedNames();

            foreach (var defaultRole in await _roleManager.Roles.Where(r => r.IsDefault).ToListAsync())
            {
                user.Roles.Add(new UserRole(AbpSession.TenantId, user.Id, defaultRole.Id));
            }

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user));
            CheckErrors(await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo(externalUser.Provider, externalUser.ProviderKey, externalUser.Provider)
            ));

            await CurrentUnitOfWork.SaveChangesAsync();
            return user;
        }

        private async Task<string> EnsureUniqueUserNameAsync(string emailAddress)
        {
            var baseName = emailAddress;
            if (await _userManager.FindByNameAsync(baseName) == null)
            {
                return baseName;
            }

            for (var i = 1; i < 1000; i++)
            {
                var candidate = $"{baseName}_{i}";
                if (await _userManager.FindByNameAsync(candidate) == null)
                {
                    return candidate;
                }
            }

            return $"{baseName}_{Guid.NewGuid():N}".Substring(0, Math.Min(256, baseName.Length + 33));
        }

        private async Task<ExternalAuthUserInfo> GetExternalUserInfo(ExternalAuthenticateModel model)
        {
            var userInfo = await _externalAuthManager.GetUserInfo(model.AuthProvider, model.ProviderAccessCode);
            if (!string.IsNullOrWhiteSpace(model.ProviderKey) &&
                !string.Equals(userInfo.ProviderKey, model.ProviderKey, System.StringComparison.Ordinal))
            {
                throw new UserFriendlyException("Could not authenticate external user.");
            }

            return userInfo;
        }

        private string GetTenancyNameByTenantId(int? tenantId)
        {
            if (!tenantId.HasValue)
            {
                return null;
            }

            return _tenantCache.GetOrNull(tenantId.Value)?.TenancyName;
        }

        private async Task<string> ResolveTenancyNameForLoginAsync(string userNameOrEmailAddress)
        {
            var user = await FindUniqueActiveUserByLoginAsync(userNameOrEmailAddress);
            return GetTenancyNameByTenantId(user?.TenantId);
        }

        private async Task<User> FindUniqueActiveUserByLoginAsync(string userNameOrEmailAddress)
        {
            var input = (userNameOrEmailAddress ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var normalizedUserName = _userManager.NormalizeName(input);
                var normalizedEmail = _userManager.NormalizeEmail(input);

                var users = await _userManager.Users
                    .Where(u =>
                        u.NormalizedUserName == normalizedUserName ||
                        u.NormalizedEmailAddress == normalizedEmail)
                    .ToListAsync();

                var activeUsers = users.Where(u => u.IsActive).ToList();
                if (activeUsers.Count == 0)
                {
                    return null;
                }

                if (activeUsers.Count > 1)
                {
                    throw new UserFriendlyException(
                        "Multiple accounts found for this login. Contact your administrator.");
                }

                return activeUsers[0];
            }
        }

        private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(
            string usernameOrEmailAddress,
            string password,
            string tenancyName)
        {
            var loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName);

            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    return loginResult;
                default:
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                        loginResult.Result,
                        usernameOrEmailAddress,
                        tenancyName);
            }
        }

        private string CreateAccessToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
        {
            var now = DateTime.UtcNow;

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration.Issuer,
                audience: _configuration.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(expiration ?? _configuration.Expiration),
                signingCredentials: _configuration.SigningCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        private static List<Claim> CreateJwtClaims(ClaimsIdentity identity)
        {
            var claims = identity.Claims.ToList();
            var nameIdClaim = claims.First(c => c.Type == ClaimTypes.NameIdentifier);

            claims.AddRange(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, nameIdClaim.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            });

            return claims;
        }

        private string GetEncryptedAccessToken(string accessToken)
        {
            return SimpleStringCipher.Instance.Encrypt(accessToken);
        }

        private void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}

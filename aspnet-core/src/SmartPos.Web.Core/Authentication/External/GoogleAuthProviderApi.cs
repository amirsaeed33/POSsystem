using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.UI;
using Google.Apis.Auth;

namespace SmartPos.Authentication.External
{
    public class GoogleAuthProviderApi : ExternalAuthProviderApiBase
    {
        public const string ProviderName = "Google";

        public override async Task<ExternalAuthUserInfo> GetUserInfo(string accessCode)
        {
            if (string.IsNullOrWhiteSpace(accessCode))
            {
                throw new UserFriendlyException("Google token is required.");
            }

            // Google Identity Services ID token (JWT) or OAuth access token.
            if (accessCode.Split('.').Length == 3)
            {
                return await GetUserInfoFromIdTokenAsync(accessCode);
            }

            return await GetUserInfoFromAccessTokenAsync(accessCode);
        }

        private async Task<ExternalAuthUserInfo> GetUserInfoFromIdTokenAsync(string idToken)
        {
            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { ProviderInfo.ClientId }
                    }
                );
            }
            catch (InvalidJwtException ex)
            {
                throw new UserFriendlyException("Invalid Google token: " + ex.Message);
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new UserFriendlyException("Google account email is required.");
            }

            return new ExternalAuthUserInfo
            {
                ProviderKey = payload.Subject,
                Name = string.IsNullOrWhiteSpace(payload.GivenName) ? payload.Name : payload.GivenName,
                Surname = string.IsNullOrWhiteSpace(payload.FamilyName) ? "-" : payload.FamilyName,
                EmailAddress = payload.Email,
                Provider = ProviderName
            };
        }

        private async Task<ExternalAuthUserInfo> GetUserInfoFromAccessTokenAsync(string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                using (var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo"))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UserFriendlyException("Could not validate Google access token.");
                    }

                    using (var doc = JsonDocument.Parse(body))
                    {
                        var root = doc.RootElement;
                        var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                        var sub = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;

                        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sub))
                        {
                            throw new UserFriendlyException("Google account email is required.");
                        }

                        var givenName = root.TryGetProperty("given_name", out var givenProp)
                            ? givenProp.GetString()
                            : null;
                        var familyName = root.TryGetProperty("family_name", out var familyProp)
                            ? familyProp.GetString()
                            : null;
                        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                        return new ExternalAuthUserInfo
                        {
                            ProviderKey = sub,
                            Name = string.IsNullOrWhiteSpace(givenName) ? (name ?? email) : givenName,
                            Surname = string.IsNullOrWhiteSpace(familyName) ? "-" : familyName,
                            EmailAddress = email,
                            Provider = ProviderName
                        };
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Emailing.Dto;

namespace SmartPos.Emailing
{
    [AbpAuthorize(PermissionNames.Pages_EmailTemplates)]
    public class EmailTemplateAppService :
        AsyncCrudAppService<EmailTemplate, EmailTemplateDto, int, PagedEmailTemplateResultRequestDto, CreateEmailTemplateDto, EmailTemplateDto>,
        IEmailTemplateAppService
    {
        public EmailTemplateAppService(IRepository<EmailTemplate> repository)
            : base(repository)
        {
        }

        public PreviewEmailTemplateOutput Preview(PreviewEmailTemplateInput input)
        {
            var values = input.SampleValues ?? DefaultSampleValues();
            return new PreviewEmailTemplateOutput
            {
                Subject = EmailTemplateRenderer.Render(input.Subject, values),
                BodyHtml = EmailTemplateRenderer.Render(input.BodyHtml, values)
            };
        }

        public override async Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateDto input)
        {
            Normalize(input);
            await EnsureUniqueCodeAsync(input.Code, excludeId: null);
            return await base.CreateAsync(input);
        }

        public override async Task<EmailTemplateDto> UpdateAsync(EmailTemplateDto input)
        {
            Normalize(input);
            await EnsureUniqueCodeAsync(input.Code, excludeId: input.Id);

            var entity = await GetEntityByIdAsync(input.Id);
            entity.Name = input.Name;
            entity.Code = input.Code;
            entity.Subject = input.Subject;
            entity.BodyHtml = input.BodyHtml;
            entity.Description = input.Description;
            entity.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(entity);
        }

        protected override IQueryable<EmailTemplate> CreateFilteredQuery(PagedEmailTemplateResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.Code.Contains(input.Keyword)
                         || (x.Subject != null && x.Subject.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        private async Task EnsureUniqueCodeAsync(string code, int? excludeId)
        {
            var exists = await Repository.GetAll()
                .AnyAsync(x => x.Code == code && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
            {
                throw new UserFriendlyException($"An email template with code \"{code}\" already exists.");
            }
        }

        private static void Normalize(CreateEmailTemplateDto input)
        {
            input.Name = input.Name?.Trim();
            input.Code = NormalizeCode(input.Code);
            input.Subject = input.Subject?.Trim();
            input.Description = input.Description?.Trim();
        }

        private static void Normalize(EmailTemplateDto input)
        {
            input.Name = input.Name?.Trim();
            input.Code = NormalizeCode(input.Code);
            input.Subject = input.Subject?.Trim();
            input.Description = input.Description?.Trim();
        }

        private static string NormalizeCode(string code)
        {
            return (code ?? string.Empty).Trim().Replace(" ", string.Empty);
        }

        public static Dictionary<string, string> DefaultSampleValues()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Code"] = "123456",
                ["ExpirationMinutes"] = "5",
                ["UserName"] = "admin",
                ["Name"] = "Admin",
                ["Email"] = "admin@example.com",
                ["AppName"] = "SmartPos"
            };
        }
    }
}

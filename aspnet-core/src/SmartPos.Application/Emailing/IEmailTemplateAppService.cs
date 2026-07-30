using Abp.Application.Services;
using SmartPos.Emailing.Dto;

namespace SmartPos.Emailing
{
    public interface IEmailTemplateAppService :
        IAsyncCrudAppService<EmailTemplateDto, int, PagedEmailTemplateResultRequestDto, CreateEmailTemplateDto, EmailTemplateDto>
    {
        PreviewEmailTemplateOutput Preview(PreviewEmailTemplateInput input);
    }
}

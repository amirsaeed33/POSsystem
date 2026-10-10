using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace SmartPos.Branches
{
    public interface IBranchEmailNotifier : IDomainService
    {
        Task SendEmailsAsync(IEnumerable<string> emails, string subject, string body);
        Task SendEmailsAsync(string emailsCsv, string subject, string body);
    }
}

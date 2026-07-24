using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Accounts.Dto;

namespace SmartPos.Accounts
{
    public interface ILedgerEntryAppService : IAsyncCrudAppService<LedgerEntryDto, int, PagedLedgerEntryResultRequestDto, CreateLedgerEntryDto, LedgerEntryDto>
    {
        Task<AccountBalanceDto> GetBalance(EntityDto input);
    }
}

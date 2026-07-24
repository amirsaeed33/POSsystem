using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Reports.Dto;

namespace SmartPos.Reports
{
    public interface IReportAppService : IApplicationService
    {
        Task<SaleReportDto> GetSaleReportAsync(ReportDateRangeInput input);

        Task<PurchaseReportDto> GetPurchaseReportAsync(ReportDateRangeInput input);

        Task<ExpenseReportDto> GetExpenseReportAsync(ReportDateRangeInput input);

        Task<StockReportDto> GetStockReportAsync(ReportDateRangeInput input);
    }
}

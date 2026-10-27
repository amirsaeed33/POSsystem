using System.Threading.Tasks;
using Abp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using SmartPos.Reports.Dto;

namespace SmartPos.Reports
{
    public interface IReportAppService : IApplicationService
    {
        Task<SaleReportDto> GetSaleReportAsync(ReportDateRangeInput input);

        Task<PurchaseReportDto> GetPurchaseReportAsync(ReportDateRangeInput input);

        Task<ExpenseReportDto> GetExpenseReportAsync(ReportDateRangeInput input);

        Task<StockReportDto> GetStockReportAsync(ReportDateRangeInput input);

        Task<ProductProfitReportDto> GetProductProfitReportAsync(ReportDateRangeInput input);

        Task<BalanceSheetReportDto> GetBalanceSheetReportAsync(ReportDateRangeInput input);

        Task<FileContentResult> GetSaleReportPdf(ReportDateRangeInput input);

        Task<FileContentResult> GetPurchaseReportPdf(ReportDateRangeInput input);

        Task<FileContentResult> GetExpenseReportPdf(ReportDateRangeInput input);

        Task<FileContentResult> GetStockReportPdf(ReportDateRangeInput input);

        Task<FileContentResult> GetProductProfitReportPdf(ReportDateRangeInput input);

        Task<FileContentResult> GetBalanceSheetReportPdf(ReportDateRangeInput input);
    }
}

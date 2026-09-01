using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Branches;
using SmartPos.Emailing;
using SmartPos.Expenses;
using SmartPos.Inventory;
using SmartPos.NotificationEmails.Dto;
using SmartPos.Products;
using SmartPos.Purchases;
using SmartPos.Sales;

namespace SmartPos.NotificationEmails
{
    [AbpAuthorize]
    public class NotificationEmailAppService : SmartPosAppServiceBase, INotificationEmailAppService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<SaleLine> _saleLineRepository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly ISmtpMailSender _smtpMailSender;

        public NotificationEmailAppService(
            IRepository<Product> productRepository,
            IRepository<BranchStock> branchStockRepository,
            IRepository<Sale> saleRepository,
            IRepository<SaleLine> saleLineRepository,
            IRepository<Purchase> purchaseRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Branch> branchRepository,
            ISmtpMailSender smtpMailSender)
        {
            _productRepository = productRepository;
            _branchStockRepository = branchStockRepository;
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _purchaseRepository = purchaseRepository;
            _expenseRepository = expenseRepository;
            _branchRepository = branchRepository;
            _smtpMailSender = smtpMailSender;
        }

        public async Task SendLowStockReportAsync(SendNotificationEmailInput input)
        {
            var targetEmail = await ResolveTargetEmailAsync(input?.TargetEmail);
            var branchId = input?.BranchId;

            var stocksQuery = _branchStockRepository.GetAll()
                .Include(bs => bs.Product)
                .Include(bs => bs.Branch)
                .Where(bs => bs.Quantity <= bs.Product.AlertQuantityLimit);

            if (branchId.HasValue && branchId.Value > 0)
            {
                stocksQuery = stocksQuery.Where(bs => bs.BranchId == branchId.Value);
            }

            var lowStockItems = await stocksQuery
                .OrderBy(bs => bs.Quantity)
                .Take(100)
                .ToListAsync();

            var branchName = branchId.HasValue && branchId.Value > 0
                ? (await _branchRepository.FirstOrDefaultAsync(branchId.Value))?.Name ?? "Selected Branch"
                : "All Branches";

            var subject = $"[SmartPOS] Low Stock Alert Report - {branchName} ({DateTime.Now:yyyy-MM-dd})";
            var bodyHtml = BuildLowStockEmailHtml(lowStockItems, branchName);

            await _smtpMailSender.SendAsync(targetEmail, subject, bodyHtml, isBodyHtml: true);
        }

        public async Task SendDailyBusinessSummaryAsync(SendNotificationEmailInput input)
        {
            var targetEmail = await ResolveTargetEmailAsync(input?.TargetEmail);
            var branchId = input?.BranchId;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Query Today's Sales
            var salesQuery = _saleRepository.GetAll()
                .Where(s => s.CreationTime >= today && s.CreationTime < tomorrow);

            if (branchId.HasValue && branchId.Value > 0)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
            }

            var todaySales = await salesQuery.ToListAsync();

            // Query Today's Purchases
            var purchasesQuery = _purchaseRepository.GetAll()
                .Where(p => p.CreationTime >= today && p.CreationTime < tomorrow);

            if (branchId.HasValue && branchId.Value > 0)
            {
                purchasesQuery = purchasesQuery.Where(p => p.BranchId == branchId.Value);
            }

            var todayPurchases = await purchasesQuery.ToListAsync();

            // Query Today's Expenses
            var expensesQuery = _expenseRepository.GetAll()
                .Where(e => e.CreationTime >= today && e.CreationTime < tomorrow);

            if (branchId.HasValue && branchId.Value > 0)
            {
                expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
            }

            var todayExpenses = await expensesQuery.ToListAsync();

            // Query Low Stock Count
            var lowStockCountQuery = _branchStockRepository.GetAll()
                .Where(bs => bs.Quantity <= bs.Product.AlertQuantityLimit);

            if (branchId.HasValue && branchId.Value > 0)
            {
                lowStockCountQuery = lowStockCountQuery.Where(bs => bs.BranchId == branchId.Value);
            }

            var lowStockCount = await lowStockCountQuery.CountAsync();

            // Compute Summary Aggregates
            var totalSalesAmount = todaySales.Sum(s => s.TotalAmount);
            var totalDiscountAmount = todaySales.Sum(s => s.DiscountAmount);
            var netSalesAmount = totalSalesAmount - totalDiscountAmount;
            var salesCount = todaySales.Count;

            var cashSales = todaySales.Sum(s => s.CashAmount);
            var cardSales = todaySales.Sum(s => s.CardAmount);
            var creditSales = todaySales.Sum(s => s.CreditAmount);
            var bankSales = todaySales.Where(s => s.PaymentType == PaymentTypes.Mixed).Sum(s => s.TotalAmount - s.CashAmount - s.CardAmount - s.CreditAmount);

            var totalPurchasesAmount = todayPurchases.Sum(p => p.TotalAmount);
            var totalExpensesAmount = todayExpenses.Sum(e => e.Amount);

            var branchName = branchId.HasValue && branchId.Value > 0
                ? (await _branchRepository.FirstOrDefaultAsync(branchId.Value))?.Name ?? "Selected Branch"
                : "All Branches";

            var subject = $"[SmartPOS] Daily Business Summary - {branchName} ({today:yyyy-MM-dd})";
            var bodyHtml = BuildDailySummaryEmailHtml(
                branchName,
                today,
                salesCount,
                totalSalesAmount,
                totalDiscountAmount,
                netSalesAmount,
                cashSales,
                cardSales,
                bankSales,
                creditSales,
                todayPurchases.Count,
                totalPurchasesAmount,
                todayExpenses.Count,
                totalExpensesAmount,
                lowStockCount
            );

            await _smtpMailSender.SendAsync(targetEmail, subject, bodyHtml, isBodyHtml: true);
        }

        private async Task<string> ResolveTargetEmailAsync(string providedEmail)
        {
            var email = (providedEmail ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            if (AbpSession.UserId.HasValue)
            {
                var user = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                if (!string.IsNullOrWhiteSpace(user?.EmailAddress))
                {
                    return user.EmailAddress;
                }
            }

            throw new UserFriendlyException("Please specify a recipient email address.");
        }

        private string BuildLowStockEmailHtml(List<BranchStock> lowStockItems, string branchName)
        {
            var sb = new StringBuilder();
            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }}
        .container {{ max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .header {{ background: #dc2626; padding: 24px; color: #ffffff; text-align: center; }}
        .header h2 {{ margin: 0; font-size: 22px; font-weight: 600; }}
        .header p {{ margin: 6px 0 0 0; opacity: 0.9; font-size: 14px; }}
        .content {{ padding: 24px; }}
        .badge {{ background: #fee2e2; color: #991b1b; font-size: 12px; font-weight: 600; padding: 4px 10px; border-radius: 12px; display: inline-block; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 14px; }}
        th {{ background: #f8fafc; color: #475569; text-align: left; padding: 10px 12px; border-bottom: 2px solid #e2e8f0; font-weight: 600; }}
        td {{ padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }}
        .low-qty {{ color: #dc2626; font-weight: bold; }}
        .footer {{ background: #f8fafc; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>⚠️ Low Stock Alert Report</h2>
            <p>{branchName} · {DateTime.Now:MMMM dd, yyyy}</p>
        </div>
        <div class='content'>
            <p>The following <strong>{lowStockItems.Count} item(s)</strong> have reached or dropped below their minimum alert quantity limit:</p>
            <table>
                <thead>
                    <tr>
                        <th>Product</th>
                        <th>Barcode</th>
                        <th>Branch</th>
                        <th style='text-align:right;'>Stock</th>
                        <th style='text-align:right;'>Alert Limit</th>
                    </tr>
                </thead>
                <tbody>");

            if (!lowStockItems.Any())
            {
                sb.Append("<tr><td colspan='5' style='text-align:center; padding:20px; color:#64748b;'>All products are sufficiently stocked.</td></tr>");
            }
            else
            {
                foreach (var item in lowStockItems)
                {
                    sb.Append($@"
                    <tr>
                        <td><strong>{System.Net.WebUtility.HtmlEncode(item.Product?.Name ?? "N/A")}</strong></td>
                        <td><code>{System.Net.WebUtility.HtmlEncode(item.Product?.Barcode ?? "N/A")}</code></td>
                        <td>{System.Net.WebUtility.HtmlEncode(item.Branch?.Name ?? "Main")}</td>
                        <td style='text-align:right;' class='low-qty'>{item.Quantity:N0}</td>
                        <td style='text-align:right;'>{item.Product?.AlertQuantityLimit:N0}</td>
                    </tr>");
                }
            }

            sb.Append(@"
                </tbody>
            </table>
        </div>
        <div class='footer'>
            Sent automatically by <strong>SmartPOS System</strong>. Please reorder depleted items promptly.
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        private string BuildDailySummaryEmailHtml(
            string branchName,
            DateTime date,
            int salesCount,
            decimal totalSalesAmount,
            decimal totalDiscountAmount,
            decimal netSalesAmount,
            decimal cashSales,
            decimal cardSales,
            decimal bankSales,
            decimal creditSales,
            int purchasesCount,
            decimal totalPurchasesAmount,
            int expensesCount,
            decimal totalExpensesAmount,
            int lowStockCount)
        {
            var sb = new StringBuilder();
            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }}
        .container {{ max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .header {{ background: #2563eb; padding: 24px; color: #ffffff; text-align: center; }}
        .header h2 {{ margin: 0; font-size: 22px; font-weight: 600; }}
        .header p {{ margin: 6px 0 0 0; opacity: 0.9; font-size: 14px; }}
        .content {{ padding: 24px; }}
        .grid {{ display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; margin-bottom: 20px; }}
        .card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 14px; text-align: center; }}
        .card-title {{ font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600; margin-bottom: 4px; }}
        .card-value {{ font-size: 20px; color: #0f172a; font-weight: 700; }}
        .card-sub {{ font-size: 11px; color: #94a3b8; margin-top: 2px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 14px; }}
        th {{ background: #f8fafc; color: #475569; text-align: left; padding: 10px 12px; border-bottom: 2px solid #e2e8f0; font-weight: 600; }}
        td {{ padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }}
        .footer {{ background: #f8fafc; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📊 Daily Business Summary</h2>
            <p>{branchName} · {date:MMMM dd, yyyy}</p>
        </div>
        <div class='content'>
            <div class='grid'>
                <div class='card'>
                    <div class='card-title'>Net Sales</div>
                    <div class='card-value' style='color:#16a34a;'>{netSalesAmount:C2}</div>
                    <div class='card-sub'>{salesCount} sales completed</div>
                </div>
                <div class='card'>
                    <div class='card-title'>Purchases</div>
                    <div class='card-value' style='color:#2563eb;'>{totalPurchasesAmount:C2}</div>
                    <div class='card-sub'>{purchasesCount} purchase orders</div>
                </div>
                <div class='card'>
                    <div class='card-title'>Expenses</div>
                    <div class='card-value' style='color:#dc2626;'>{totalExpensesAmount:C2}</div>
                    <div class='card-sub'>{expensesCount} expense records</div>
                </div>
                <div class='card'>
                    <div class='card-title'>Low Stock Alert</div>
                    <div class='card-value' style='color:#d97706;'>{lowStockCount}</div>
                    <div class='card-sub'>Items needing reorder</div>
                </div>
            </div>

            <h3 style='font-size:16px; color:#1e293b; margin-top:24px; margin-bottom:8px;'>Payment Method Breakdown</h3>
            <table>
                <thead>
                    <tr>
                        <th>Payment Method</th>
                        <th style='text-align:right;'>Amount</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>💵 Cash</td>
                        <td style='text-align:right;'><strong>{cashSales:C2}</strong></td>
                    </tr>
                    <tr>
                        <td>💳 Card</td>
                        <td style='text-align:right;'><strong>{cardSales:C2}</strong></td>
                    </tr>
                    <tr>
                        <td>🏦 Bank Transfer</td>
                        <td style='text-align:right;'><strong>{bankSales:C2}</strong></td>
                    </tr>
                    <tr>
                        <td>📝 Credit Account</td>
                        <td style='text-align:right;'><strong>{creditSales:C2}</strong></td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class='footer'>
            Generated automatically by <strong>SmartPOS System</strong>.
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}

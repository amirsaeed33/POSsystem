using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public class AdminDashboardViewModel : BaseViewModel
{
    private readonly DashboardService _dashboard;
    private readonly SessionService _session;

    private string _greeting = "Admin";
    private string _salesTodayText = "Rs 0";
    private string _purchasesTodayText = "Rs 0";
    private string _profitTodayText = "Rs 0";
    private string _growthText = "0%";
    private string _pendingOrdersText = "0";
    private string _todayOrdersText = "0";
    private string _preparingText = "0";
    private string _deliveredText = "0";
    private string _inStockText = "0";
    private string _lowStockText = "0";
    private string _outStockText = "0";
    private string _branchCountText = "0";
    private string _statusSummary = "Loading restaurant status…";
    private string _errorText = string.Empty;

    public AdminDashboardViewModel(DashboardService dashboard, SessionService session)
    {
        _dashboard = dashboard;
        _session = session;
        RefreshCommand = new Command(async () => await LoadAsync());
        DayBars = new ObservableCollection<AdminDayBarItem>();
        Timeline = new ObservableCollection<AdminTimelineItem>();
        PipelineStages = new ObservableCollection<OnlineOrderPipelineStageDto>();
    }

    public string Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public string SalesTodayText
    {
        get => _salesTodayText;
        set => SetProperty(ref _salesTodayText, value);
    }

    public string PurchasesTodayText
    {
        get => _purchasesTodayText;
        set => SetProperty(ref _purchasesTodayText, value);
    }

    public string ProfitTodayText
    {
        get => _profitTodayText;
        set => SetProperty(ref _profitTodayText, value);
    }

    public string GrowthText
    {
        get => _growthText;
        set => SetProperty(ref _growthText, value);
    }

    public string PendingOrdersText
    {
        get => _pendingOrdersText;
        set => SetProperty(ref _pendingOrdersText, value);
    }

    public string TodayOrdersText
    {
        get => _todayOrdersText;
        set => SetProperty(ref _todayOrdersText, value);
    }

    public string PreparingText
    {
        get => _preparingText;
        set => SetProperty(ref _preparingText, value);
    }

    public string DeliveredText
    {
        get => _deliveredText;
        set => SetProperty(ref _deliveredText, value);
    }

    public string InStockText
    {
        get => _inStockText;
        set => SetProperty(ref _inStockText, value);
    }

    public string LowStockText
    {
        get => _lowStockText;
        set => SetProperty(ref _lowStockText, value);
    }

    public string OutStockText
    {
        get => _outStockText;
        set => SetProperty(ref _outStockText, value);
    }

    public string BranchCountText
    {
        get => _branchCountText;
        set => SetProperty(ref _branchCountText, value);
    }

    public string StatusSummary
    {
        get => _statusSummary;
        set => SetProperty(ref _statusSummary, value);
    }

    public string ErrorText
    {
        get => _errorText;
        set
        {
            if (SetProperty(ref _errorText, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public ObservableCollection<AdminDayBarItem> DayBars { get; }
    public ObservableCollection<AdminTimelineItem> Timeline { get; }
    public ObservableCollection<OnlineOrderPipelineStageDto> PipelineStages { get; }

    public ICommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorText = string.Empty;
            Greeting = string.IsNullOrWhiteSpace(_session.User?.DisplayName)
                ? "Admin dashboard"
                : $"Hi, {_session.User!.DisplayName}";

            var dashTask = _dashboard.GetDashboardAsync();
            var pipeTask = _dashboard.GetOnlineOrderPipelineAsync();
            await Task.WhenAll(dashTask, pipeTask);

            var dash = await dashTask;
            var pipe = await pipeTask;

            SalesTodayText = FormatMoney(dash.TodaySales);
            PurchasesTodayText = FormatMoney(dash.TodayPurchases);
            ProfitTodayText = FormatMoney(dash.TodayProfit);
            GrowthText = $"{(dash.IsGrowthPositive ? "+" : "")}{dash.GrowthPercentage:0.#}%";

            InStockText = dash.InStockCount.ToString();
            LowStockText = dash.LowStockCount.ToString();
            OutStockText = dash.OutOfStockCount.ToString();
            BranchCountText = (dash.QuickActions?.BranchCount ?? 0).ToString();

            PendingOrdersText = pipe.PendingCount.ToString();
            TodayOrdersText = pipe.TodayTotalOrders.ToString();
            PreparingText = pipe.InProgressCount.ToString();
            DeliveredText = pipe.DeliveredCount.ToString();

            var open = pipe.PendingCount + pipe.ApprovedCount + pipe.InProgressCount;
            StatusSummary = open == 0
                ? "Restaurant is clear — no open online orders right now."
                : $"{open} open online order{(open == 1 ? "" : "s")} · {dash.LowStockCount} low-stock item{(dash.LowStockCount == 1 ? "" : "s")}";

            DayBars.Clear();
            var days = (dash.Last7Days ?? new List<DailyOverviewDto>()).TakeLast(7).ToList();
            var max = days.Select(d => Math.Max(d.Sales, d.Purchases)).DefaultIfEmpty(1).Max();
            if (max <= 0)
            {
                max = 1;
            }

            foreach (var day in days)
            {
                DayBars.Add(new AdminDayBarItem
                {
                    DayLabel = string.IsNullOrWhiteSpace(day.DayLabel)
                        ? day.Date.ToString("ddd")
                        : day.DayLabel!,
                    SalesBarHeight = Math.Max(4, (double)(day.Sales / max) * 72),
                    PurchasesBarHeight = Math.Max(4, (double)(day.Purchases / max) * 72),
                    SalesText = FormatMoney(day.Sales),
                    PurchasesText = FormatMoney(day.Purchases)
                });
            }

            PipelineStages.Clear();
            foreach (var stage in pipe.Stages ?? new List<OnlineOrderPipelineStageDto>())
            {
                PipelineStages.Add(stage);
            }

            Timeline.Clear();
            foreach (var item in (dash.Timeline ?? new List<DashboardTimelineEventDto>()).Take(12))
            {
                Timeline.Add(new AdminTimelineItem
                {
                    Title = item.Title ?? item.Type ?? "Activity",
                    Subtitle = item.OccurredAt.ToLocalTime().ToString("dd MMM · HH:mm"),
                    AmountText = FormatMoney(item.Amount),
                    Accent = item.Type?.ToLowerInvariant() switch
                    {
                        "sale" => Color.FromArgb("#0F6B4C"),
                        "purchase" => Color.FromArgb("#C2410C"),
                        "expense" => Color.FromArgb("#B91C1C"),
                        _ => Color.FromArgb("#475569")
                    }
                });
            }
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            StatusSummary = "Could not load restaurant status.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatMoney(decimal value) => $"Rs {value:0.##}";
}

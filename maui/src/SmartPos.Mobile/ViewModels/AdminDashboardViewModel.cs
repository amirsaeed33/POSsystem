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
    private string _headerSubtitle = "Operations overview";
    private string _salesTodayText = "Rs 0";
    private string _purchasesTodayText = "Rs 0";
    private string _profitTodayText = "Rs 0";
    private string _expensesTodayText = "Rs 0";
    private string _growthText = "+0%";
    private string _salesCountText = "0 sales today";
    private string _pendingOrdersText = "0";
    private string _todayOrdersText = "0";
    private string _openOrdersText = "0";
    private string _preparingText = "0";
    private string _deliveredText = "0";
    private string _inStockText = "0";
    private string _lowStockText = "0";
    private string _outStockText = "0";
    private string _branchCountText = "0";
    private string _statusSummary = "Loading restaurant status…";
    private string _errorText = string.Empty;
    private string _chartEmptyText = "No sales or purchases in the last 7 days.";
    private string _topAreaSummary = "Loading delivery areas…";
    private bool _hasChartData;
    private bool _hasTimeline;
    private bool _hasAreaStats;
    private Color _growthColor = Color.FromArgb("#86EFAC");

    public AdminDashboardViewModel(DashboardService dashboard, SessionService session)
    {
        _dashboard = dashboard;
        _session = session;
        RefreshCommand = new Command(async () => await LoadAsync());
        DayBars = new ObservableCollection<AdminDayBarItem>();
        Timeline = new ObservableCollection<AdminTimelineItem>();
        PipelineStages = new ObservableCollection<OnlineOrderPipelineStageDto>();
        AreaRanks = new ObservableCollection<AdminAreaRankItem>();
    }

    public string Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public string HeaderSubtitle
    {
        get => _headerSubtitle;
        set => SetProperty(ref _headerSubtitle, value);
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

    public string ExpensesTodayText
    {
        get => _expensesTodayText;
        set => SetProperty(ref _expensesTodayText, value);
    }

    public string GrowthText
    {
        get => _growthText;
        set => SetProperty(ref _growthText, value);
    }

    public Color GrowthColor
    {
        get => _growthColor;
        set => SetProperty(ref _growthColor, value);
    }

    public string SalesCountText
    {
        get => _salesCountText;
        set => SetProperty(ref _salesCountText, value);
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

    public string OpenOrdersText
    {
        get => _openOrdersText;
        set => SetProperty(ref _openOrdersText, value);
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

    public string ChartEmptyText
    {
        get => _chartEmptyText;
        set => SetProperty(ref _chartEmptyText, value);
    }

    public bool HasChartData
    {
        get => _hasChartData;
        set
        {
            if (SetProperty(ref _hasChartData, value))
            {
                OnPropertyChanged(nameof(IsChartEmpty));
            }
        }
    }

    public bool IsChartEmpty => !HasChartData;

    public bool HasTimeline
    {
        get => _hasTimeline;
        set
        {
            if (SetProperty(ref _hasTimeline, value))
            {
                OnPropertyChanged(nameof(IsTimelineEmpty));
                OnPropertyChanged(nameof(TimelineCountText));
            }
        }
    }

    public bool IsTimelineEmpty => !HasTimeline;

    public string TimelineCountText => HasTimeline ? $"{Timeline.Count} events" : "None yet";

    public string TopAreaSummary
    {
        get => _topAreaSummary;
        set => SetProperty(ref _topAreaSummary, value);
    }

    public bool HasAreaStats
    {
        get => _hasAreaStats;
        set
        {
            if (SetProperty(ref _hasAreaStats, value))
            {
                OnPropertyChanged(nameof(IsAreaStatsEmpty));
            }
        }
    }

    public bool IsAreaStatsEmpty => !HasAreaStats;

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
    public ObservableCollection<AdminAreaRankItem> AreaRanks { get; }

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
            var name = string.IsNullOrWhiteSpace(_session.User?.DisplayName)
                ? "Admin"
                : _session.User!.DisplayName!;
            Greeting = $"Hi, {name}";
            HeaderSubtitle = $"{DateTime.Now:dddd, dd MMM} · Super admin control";

            var dashTask = _dashboard.GetDashboardAsync();
            var pipeTask = _dashboard.GetOnlineOrderPipelineAsync();
            await Task.WhenAll(dashTask, pipeTask);

            var dash = await dashTask;
            var pipe = await pipeTask;

            SalesTodayText = FormatMoney(dash.TodaySales);
            PurchasesTodayText = FormatMoney(dash.TodayPurchases);
            ProfitTodayText = FormatMoney(dash.TodayProfit);
            ExpensesTodayText = FormatMoney(dash.TodayExpenses);
            GrowthText = $"{(dash.IsGrowthPositive ? "+" : "")}{dash.GrowthPercentage:0.#}%";
            GrowthColor = dash.IsGrowthPositive
                ? Color.FromArgb("#86EFAC")
                : Color.FromArgb("#FCA5A5");
            SalesCountText = $"{dash.QuickActions?.TodaySalesCount ?? 0} sales today";

            InStockText = dash.InStockCount.ToString();
            LowStockText = dash.LowStockCount.ToString();
            OutStockText = dash.OutOfStockCount.ToString();
            BranchCountText = (dash.QuickActions?.BranchCount ?? 0).ToString();

            PendingOrdersText = pipe.PendingCount.ToString();
            TodayOrdersText = pipe.TodayTotalOrders.ToString();
            PreparingText = pipe.InProgressCount.ToString();
            DeliveredText = pipe.DeliveredCount.ToString();

            var open = pipe.PendingCount + pipe.ApprovedCount + pipe.InProgressCount;
            OpenOrdersText = open.ToString();
            StatusSummary = open == 0
                ? "Restaurant is clear — no open online orders right now."
                : $"{open} open online order{(open == 1 ? "" : "s")} · {dash.LowStockCount} low-stock item{(dash.LowStockCount == 1 ? "" : "s")}";

            DayBars.Clear();
            var days = (dash.Last7Days ?? new List<DailyOverviewDto>()).TakeLast(7).ToList();
            var max = days.Select(d => Math.Max(d.Sales, d.Purchases)).DefaultIfEmpty(0).Max();
            HasChartData = max > 0;
            if (max <= 0)
            {
                max = 1;
            }

            foreach (var day in days)
            {
                var salesH = day.Sales <= 0 ? 3 : Math.Max(8, (double)(day.Sales / max) * 100);
                var buyH = day.Purchases <= 0 ? 3 : Math.Max(8, (double)(day.Purchases / max) * 100);
                DayBars.Add(new AdminDayBarItem
                {
                    DayLabel = string.IsNullOrWhiteSpace(day.DayLabel)
                        ? day.Date.ToString("ddd")
                        : day.DayLabel!,
                    SalesBarHeight = salesH,
                    PurchasesBarHeight = buyH,
                    SalesText = FormatMoney(day.Sales),
                    PurchasesText = FormatMoney(day.Purchases)
                });
            }

            PipelineStages.Clear();
            foreach (var stage in pipe.Stages ?? new List<OnlineOrderPipelineStageDto>())
            {
                PipelineStages.Add(stage);
            }

            AreaRanks.Clear();
            var areas = (pipe.TopAreas ?? new List<OrderAreaStatDto>()).ToList();
            var maxAreaOrders = areas.Select(a => a.OrderCount).DefaultIfEmpty(0).Max();
            if (maxAreaOrders <= 0)
            {
                maxAreaOrders = 1;
            }

            for (var i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                var isTop = area.IsTopArea || i == 0;
                AreaRanks.Add(new AdminAreaRankItem
                {
                    Rank = i + 1,
                    RankLabel = $"#{i + 1}",
                    AreaName = string.IsNullOrWhiteSpace(area.AreaName) ? "Unknown area" : area.AreaName!,
                    OrderCountText = $"{area.OrderCount} order{(area.OrderCount == 1 ? "" : "s")}",
                    AmountText = FormatMoney(area.TotalAmount),
                    ShareText = $"{area.SharePercent:0.#}%",
                    BarWidth = Math.Max(12, (double)area.OrderCount / maxAreaOrders * 220),
                    IsTopArea = isTop,
                    Accent = isTop ? Color.FromArgb("#0F6B4C") : Color.FromArgb("#64748B"),
                    SoftAccent = isTop ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#F1F5F9")
                });
            }

            HasAreaStats = AreaRanks.Count > 0;
            TopAreaSummary = HasAreaStats
                ? $"Most ordered: {pipe.TopAreaName ?? AreaRanks[0].AreaName} · last 30 days ({pipe.AreaOrdersInWindow} orders)"
                : "No delivery-area orders in the last 30 days.";

            Timeline.Clear();
            foreach (var item in (dash.Timeline ?? new List<DashboardTimelineEventDto>()).Take(12))
            {
                var (accent, soft) = ResolveAccent(item.Type);
                Timeline.Add(new AdminTimelineItem
                {
                    Title = item.Title ?? item.Type ?? "Activity",
                    Subtitle = item.OccurredAt.ToLocalTime().ToString("dd MMM · HH:mm"),
                    AmountText = FormatMoney(item.Amount),
                    Accent = accent,
                    AccentSoft = soft
                });
            }

            HasTimeline = Timeline.Count > 0;
            OnPropertyChanged(nameof(TimelineCountText));
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            StatusSummary = "Could not load restaurant status.";
            HasChartData = false;
            HasTimeline = false;
            HasAreaStats = false;
            TopAreaSummary = "Could not load area stats.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static (Color Accent, Color Soft) ResolveAccent(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "sale" => (Color.FromArgb("#0F6B4C"), Color.FromArgb("#BBF7D0")),
            "purchase" => (Color.FromArgb("#C2410C"), Color.FromArgb("#FED7AA")),
            "expense" => (Color.FromArgb("#B91C1C"), Color.FromArgb("#FECACA")),
            _ => (Color.FromArgb("#475569"), Color.FromArgb("#E2E8F0"))
        };

    private static string FormatMoney(decimal value) => $"Rs {value:0.##}";
}

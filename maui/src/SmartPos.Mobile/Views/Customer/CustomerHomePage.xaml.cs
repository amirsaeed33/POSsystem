using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class CustomerHomePage : ContentPage
{
    private readonly CustomerHomeViewModel _vm;

    public CustomerHomePage() : this(ServiceHelper.Get<CustomerHomeViewModel>())
    {
    }

    public CustomerHomePage(CustomerHomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        if (_vm.AppearingCommand.CanExecute(null))
        {
            _vm.AppearingCommand.Execute(null);
        }

        if (BottomTabBar != null)
        {
            BottomTabBar.UpdateCartBadge(_vm.CartCount);
        }
    }

    protected override void OnDisappearing()
    {
        _vm.OnPageDisappearing();
        base.OnDisappearing();
    }

    private async Task AnimateAddButtonFeedbackAsync(VisualElement view)
    {
        try
        {
            await view.ScaleTo(0.85, 60, Easing.CubicIn);

            Label? lbl = view as Label;
            Border? bdr = view as Border;
            if (bdr?.Content is Label childLabel)
            {
                lbl = childLabel;
            }

            var originalText = lbl?.Text ?? "+";
            var originalBg = bdr?.BackgroundColor;

            if (lbl != null)
            {
                lbl.Text = "✓";
            }
            if (bdr != null)
            {
                bdr.BackgroundColor = Color.FromArgb("#059669");
            }

            await view.ScaleTo(1.15, 80, Easing.CubicOut);
            await Task.Delay(280);

            if (lbl != null)
            {
                lbl.Text = originalText;
            }
            if (bdr != null && originalBg != null)
            {
                bdr.BackgroundColor = originalBg;
            }

            await view.ScaleTo(1.0, 80, Easing.SpringOut);
        }
        catch
        {
            view.Scale = 1.0;
        }
    }

    private void OnAddToCartButtonTapped(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement view)
        {
            // 1. Instant non-blocking button state feedback
            _ = AnimateAddButtonFeedbackAsync(view);

            // 2. Start position from tap or view bounds
            var tapPos = e.GetPosition(FlyAnimationOverlay);
            Point startPoint;
            if (tapPos.HasValue && tapPos.Value.X > 0 && tapPos.Value.Y > 0)
            {
                startPoint = new Point(tapPos.Value.X - 12, tapPos.Value.Y - 12);
            }
            else
            {
                startPoint = GetElementScreenCoordinates(view);
            }

            // 3. Launch butter-smooth curved fly animation directly to BottomTabBar Cart icon
            _ = PlayFlyToCartAnimationAsync(startPoint);
        }
    }

    private void OnModalAddToCartClicked(object sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            _ = AnimateAddButtonFeedbackAsync(view);

            var startX = (Width > 0 ? Width : 360) / 2.0 - 12;
            var startY = (Height > 0 ? Height : 640) * 0.65 - 12;
            _ = PlayFlyToCartAnimationAsync(new Point(startX, startY));
        }
    }

    private async Task PlayFlyToCartAnimationAsync(Point start)
    {
        try
        {
            if (FlyAnimationOverlay == null)
            {
                return;
            }

            // Target position: exact center of bottom navigation Cart tab icon
            double endX = (Width > 0 ? Width : 360) * 0.375 - 12;
            double endY = (Height > 0 ? Height : 640) - 44 - 12;

            // Spawn lightweight 24x24 circular element
            var dot = new Border
            {
                WidthRequest = 24,
                HeightRequest = 24,
                StrokeThickness = 1.5,
                Stroke = Colors.White,
                BackgroundColor = Color.FromArgb("#0F6B4C"),
                InputTransparent = true,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                Content = new Label
                {
                    Text = "🛒",
                    FontSize = 11,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.White
                }
            };

            AbsoluteLayout.SetLayoutBounds(dot, new Rect(0, 0, 24, 24));
            AbsoluteLayout.SetLayoutFlags(dot, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
            dot.TranslationX = start.X;
            dot.TranslationY = start.Y;
            dot.Scale = 1.0;
            dot.Opacity = 1.0;
            FlyAnimationOverlay.Children.Add(dot);

            var tcs = new TaskCompletionSource<bool>();

            var anim = new Animation();
            // Simultaneous X translation with SinOut (smooth horizontal spread towards cart column)
            anim.Add(0, 1, new Animation(v => dot.TranslationX = v, start.X, endX, Easing.SinOut));
            // Simultaneous Y translation with CubicIn (parabolic descent accelerating into the cart)
            anim.Add(0, 1, new Animation(v => dot.TranslationY = v, start.Y, endY, Easing.CubicIn));
            // Scale down from 1.0 to 0.4
            anim.Add(0, 1, new Animation(v => dot.Scale = v, 1.0, 0.4, Easing.Linear));
            // Fade out gently on the final 50ms (~14% of 360ms)
            anim.Add(0.86, 1, new Animation(v => dot.Opacity = v, 1.0, 0.0, Easing.Linear));

            anim.Commit(this, $"Flyer_{Guid.NewGuid():N}", length: 360, finished: (v, c) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FlyAnimationOverlay.Children.Remove(dot);
                });
                tcs.TrySetResult(true);
            });

            await tcs.Task;

            // Cart icon impact feedback at the exact landing moment
            if (BottomTabBar != null)
            {
                await BottomTabBar.OnCartItemLandedAsync(_vm.CartCount);
            }
        }
        catch
        {
        }
    }

    private Point GetElementScreenCoordinates(VisualElement element)
    {
        double x = 0;
        double y = 0;
        Element? current = element;
        while (current is VisualElement ve && current != this && current != FlyAnimationOverlay)
        {
            x += ve.X + ve.TranslationX;
            y += ve.Y + ve.TranslationY;
            if (ve is ScrollView sv)
            {
                x -= sv.ScrollX;
                y -= sv.ScrollY;
            }
            current = ve.Parent;
        }

        x += (element.Width / 2.0) - 12;
        y += (element.Height / 2.0) - 12;

        return new Point(x > 0 ? x : (Width / 2.0) - 12, y > 0 ? y : (Height / 2.0) - 12);
    }

    private async void OnFavoriteHeartTapped(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.ScaleTo(1.35, 120, Easing.CubicOut);
            await view.ScaleTo(0.9, 80, Easing.CubicIn);
            await view.ScaleTo(1.0, 100, Easing.SpringOut);
        }
    }

    private void OnFeedbackPointerPressed(object? sender, PointerEventArgs e)
    {
        _vm.PauseFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerReleased(object? sender, PointerEventArgs e)
    {
        _vm.ResumeFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerEntered(object? sender, PointerEventArgs e)
    {
        _vm.PauseFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerExited(object? sender, PointerEventArgs e)
    {
        _vm.ResumeFeedbackAutoAdvanceCommand.Execute(null);
    }
}

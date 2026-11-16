using Android.Views;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace SmartPos.Mobile.Platforms.Android;

/// <summary>
/// Forces the native Shell bottom tab strip to zero height so BubbleTabBar overlays content.
/// </summary>
public class ShellTabBarCollapse : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new ZeroHeightBottomNavAppearance(this, shellItem);

    private sealed class ZeroHeightBottomNavAppearance : ShellBottomNavViewAppearanceTracker
    {
        public ZeroHeightBottomNavAppearance(IShellContext context, ShellItem item) : base(context, item)
        {
        }

        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);
            bottomView.Visibility = ViewStates.Gone;
            var lp = bottomView.LayoutParameters;
            if (lp != null)
            {
                lp.Height = 0;
                bottomView.LayoutParameters = lp;
            }
        }
    }
}

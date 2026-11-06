using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Abp;
using Abp.Extensions;
using Abp.Notifications;
using Abp.Timing;
using Abp.Web.Security.AntiForgery;
using SmartPos.Controllers;

namespace SmartPos.Web.Host.Controllers
{
    public class HomeController : SmartPosControllerBase
    {
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IWebHostEnvironment _env;

        public HomeController(
            INotificationPublisher notificationPublisher,
            IWebHostEnvironment env)
        {
            _notificationPublisher = notificationPublisher;
            _env = env;
        }

        public IActionResult Index()
        {
            // Prefer Angular SPA when present (same-site deploy under wwwroot).
            var indexPath = Path.Combine(_env.WebRootPath ?? string.Empty, "index.html");
            if (!string.IsNullOrWhiteSpace(_env.WebRootPath) && System.IO.File.Exists(indexPath))
            {
                return PhysicalFile(indexPath, "text/html");
            }

            return Redirect("/swagger");
        }

        /// <summary>
        /// This is a demo code to demonstrate sending notification to default tenant admin and host admin uers.
        /// Don't use this code in production !!!
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<ActionResult> TestNotification(string message = "")
        {
            if (message.IsNullOrEmpty())
            {
                message = "This is a test notification, created at " + Clock.Now;
            }

            var defaultTenantAdmin = new UserIdentifier(1, 2);
            var hostAdmin = new UserIdentifier(null, 1);

            await _notificationPublisher.PublishAsync(
                "App.SimpleMessage",
                new MessageNotificationData(message),
                severity: NotificationSeverity.Info,
                userIds: new[] { defaultTenantAdmin, hostAdmin }
            );

            return Content("Sent notification: " + message);
        }
    }
}

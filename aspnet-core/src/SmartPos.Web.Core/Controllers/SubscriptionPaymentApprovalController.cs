using System.Net;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using SmartPos.Billing;
using SmartPos.Billing.Dto;

namespace SmartPos.Controllers
{
    [Route("api/[controller]/[action]")]
    public class SubscriptionPaymentApprovalController : SmartPosControllerBase
    {
        private readonly ISubscriptionBillingAppService _billing;

        public SubscriptionPaymentApprovalController(ISubscriptionBillingAppService billing)
        {
            _billing = billing;
        }

        [HttpGet]
        [AbpAllowAnonymous]
        public async Task<ContentResult> Approve(string token)
        {
            try
            {
                var result = await _billing.ApproveFromEmail(token);
                return Content(Page(result), "text/html");
            }
            catch (UserFriendlyException ex)
            {
                return Content(Page(new SubscriptionApprovalPageDto
                {
                    Message = ex.Message
                }, success: false), "text/html");
            }
        }

        private static string Page(SubscriptionApprovalPageDto result, bool success = true)
        {
            var title = success
                ? (result.AlreadyApproved ? "Already approved" : "Plan reactivated")
                : "Could not approve";
            var until = result.PaidUntil.HasValue
                ? result.PaidUntil.Value.ToString("dd.MM.yyyy")
                : null;
            var detail = WebUtility.HtmlEncode(result.Message ?? title);
            var business = WebUtility.HtmlEncode(result.TenantName ?? "");
            var plan = WebUtility.HtmlEncode(result.PlanName ?? "");
            var extra = "";
            if (!string.IsNullOrWhiteSpace(business) || !string.IsNullOrWhiteSpace(plan) || until != null)
            {
                extra = "<p style=\"color:#475569;\">" + business +
                        (string.IsNullOrWhiteSpace(plan) ? "" : " · " + plan) +
                        (until == null ? "" : "<br/>Paid until <strong>" + until + "</strong>") +
                        "</p>";
            }

            var color = success ? "#15803d" : "#b91c1c";
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" +
                   title + "</title></head><body style=\"margin:0;background:#f8fafc;font-family:Segoe UI,Arial,sans-serif;\">" +
                   "<div style=\"max-width:32rem;margin:4rem auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;padding:2rem;\">" +
                   "<h1 style=\"margin:0 0 12px;color:" + color + ";font-size:1.5rem;\">" + title + "</h1>" +
                   "<p style=\"font-size:1.05rem;\">" + detail + "</p>" + extra +
                   "</div></body></html>";
        }
    }
}

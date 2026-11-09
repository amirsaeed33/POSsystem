using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Controllers;

namespace SmartPos.Web.Host.Controllers
{
    /// <summary>
    /// Host-aware robots.txt and sitemap.xml for platform + client custom domains.
    /// </summary>
    public class SeoController : SmartPosControllerBase
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SeoController(
            IRepository<Branch> branchRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _branchRepository = branchRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 300)]
        public IActionResult Robots()
        {
            var origin = GetRequestOrigin();
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /app");
            sb.AppendLine("Disallow: /auth");
            sb.AppendLine("Disallow: /swagger");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine("Disallow: /Abp/");
            sb.AppendLine("Disallow: /online-order/history");
            sb.AppendLine("Disallow: /online-order/track");
            sb.AppendLine($"Sitemap: {origin}/sitemap.xml");
            return Content(sb.ToString(), "text/plain; charset=utf-8");
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> Sitemap()
        {
            var origin = GetRequestOrigin();
            var host = Request.Host.Host;
            var normalized = StorefrontDomainHelper.Normalize(host);

            string storefrontOrigin = origin;
            using (var uow = _unitOfWorkManager.Begin())
            {
                using (_unitOfWorkManager.Current.DisableFilter(
                           AbpDataFilters.MayHaveTenant,
                           AbpDataFilters.MustHaveTenant))
                {
                    if (!StorefrontDomainHelper.IsPlatformHost(normalized))
                    {
                        var domains = await _branchRepository.GetAll()
                            .AsNoTracking()
                            .Where(b => b.IsActive
                                        && b.CustomDomain != null
                                        && b.CustomDomain != string.Empty)
                            .Select(b => b.CustomDomain)
                            .ToListAsync();

                        var mapped = domains.FirstOrDefault(d =>
                            StorefrontDomainHelper.HostMatchesDomain(normalized, d));
                        if (!string.IsNullOrEmpty(mapped))
                        {
                            // Client domain: only this store's public URLs.
                            storefrontOrigin = origin;
                            var xml = BuildSitemapXml(storefrontOrigin, onlyStorefront: true);
                            await uow.CompleteAsync();
                            return Content(xml, "application/xml; charset=utf-8");
                        }
                    }
                }

                await uow.CompleteAsync();
            }

            // Platform host: default Al Jannat URLs.
            return Content(BuildSitemapXml(storefrontOrigin, onlyStorefront: true),
                "application/xml; charset=utf-8");
        }

        private string GetRequestOrigin()
        {
            var scheme = Request.Scheme;
            if (Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto)
                && !string.IsNullOrWhiteSpace(proto))
            {
                scheme = proto.ToString().Split(',')[0].Trim();
            }

            var host = Request.Host.Value;
            return $"{scheme}://{host}".TrimEnd('/');
        }

        private static string BuildSitemapXml(string origin, bool onlyStorefront)
        {
            var baseUrl = (origin ?? "https://aljannatapp.com").TrimEnd('/');
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            // Root redirects to online store; marketing /product is held until go-live.
            AppendUrl(sb, $"{baseUrl}/", "1.0");
            AppendUrl(sb, $"{baseUrl}/online-order", "1.0");
            if (!onlyStorefront)
            {
                // Reserved for future public pages (product landing after go-live).
            }

            sb.AppendLine("</urlset>");
            return sb.ToString();
        }

        private static void AppendUrl(StringBuilder sb, string loc, string priority)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(loc)}</loc>");
            sb.AppendLine("    <changefreq>daily</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }
    }
}

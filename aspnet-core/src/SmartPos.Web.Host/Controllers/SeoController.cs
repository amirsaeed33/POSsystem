using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Controllers;
using SmartPos.Products;

namespace SmartPos.Web.Host.Controllers
{
    /// <summary>
    /// Host-aware robots.txt and sitemap.xml for platform + client custom domains.
    /// Includes online-visible product detail URLs for Google indexing.
    /// </summary>
    public class SeoController : SmartPosControllerBase
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SeoController(
            IRepository<Branch> branchRepository,
            IRepository<Product> productRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _branchRepository = branchRepository;
            _productRepository = productRepository;
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

            using (var uow = _unitOfWorkManager.Begin())
            {
                using (_unitOfWorkManager.Current.DisableFilter(
                           AbpDataFilters.MayHaveTenant,
                           AbpDataFilters.MustHaveTenant))
                {
                    int? customDomainBranchId = null;

                    if (!StorefrontDomainHelper.IsPlatformHost(normalized))
                    {
                        var branchesWithDomain = await _branchRepository.GetAll()
                            .AsNoTracking()
                            .Where(b => b.IsActive
                                        && b.CustomDomain != null
                                        && b.CustomDomain != string.Empty)
                            .Select(b => new { b.Id, b.CustomDomain })
                            .ToListAsync();

                        customDomainBranchId = branchesWithDomain
                            .Where(b => StorefrontDomainHelper.HostMatchesDomain(normalized, b.CustomDomain))
                            .Select(b => (int?)b.Id)
                            .FirstOrDefault();
                    }

                    string xml;
                    if (customDomainBranchId.HasValue)
                    {
                        // Custom storefront domain: this branch only (no branchId query needed).
                        var products = await LoadOnlineProductsAsync(customDomainBranchId.Value);
                        xml = BuildSitemapXml(
                            origin,
                            branchScoped: true,
                            branchId: customDomainBranchId.Value,
                            products: products);
                    }
                    else
                    {
                        // Platform host: all active branches + product URLs with ?branchId=
                        var activeBranchIds = await _branchRepository.GetAll()
                            .AsNoTracking()
                            .Where(b => b.IsActive)
                            .Select(b => b.Id)
                            .ToListAsync();

                        var products = await LoadOnlineProductsAsync(branchIds: activeBranchIds);
                        xml = BuildSitemapXml(
                            origin,
                            branchScoped: false,
                            branchId: null,
                            products: products,
                            activeBranchIds: activeBranchIds);
                    }

                    await uow.CompleteAsync();
                    return Content(xml, "application/xml; charset=utf-8");
                }
            }
        }

        private async Task<List<ProductSitemapItem>> LoadOnlineProductsAsync(int branchId)
        {
            return await LoadOnlineProductsAsync(new List<int> { branchId });
        }

        private async Task<List<ProductSitemapItem>> LoadOnlineProductsAsync(List<int> branchIds)
        {
            if (branchIds == null || branchIds.Count == 0)
            {
                return new List<ProductSitemapItem>();
            }

            // Soft-delete filter remains on; only online-visible catalog items.
            return await _productRepository.GetAll()
                .AsNoTracking()
                .Where(p => p.IsVisibleOnline
                            && p.BranchId > 0
                            && branchIds.Contains(p.BranchId))
                .OrderBy(p => p.BranchId)
                .ThenBy(p => p.Id)
                .Select(p => new ProductSitemapItem
                {
                    Id = p.Id,
                    BranchId = p.BranchId,
                    LastModified = p.LastModificationTime ?? p.CreationTime
                })
                .ToListAsync();
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

        private static string BuildSitemapXml(
            string origin,
            bool branchScoped,
            int? branchId,
            List<ProductSitemapItem> products,
            List<int> activeBranchIds = null)
        {
            var baseUrl = (origin ?? "https://aljannatapp.com").TrimEnd('/');
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            AppendUrl(sb, $"{baseUrl}/", "1.0", null);

            if (branchScoped && branchId.HasValue)
            {
                AppendUrl(sb, $"{baseUrl}/online-order", "1.0", null);
                foreach (var p in products)
                {
                    AppendUrl(
                        sb,
                        $"{baseUrl}/online-order/product/{p.Id}",
                        "0.8",
                        p.LastModified);
                }
            }
            else
            {
                // Platform: storefront entry per active branch + each product with branchId.
                foreach (var bId in activeBranchIds ?? new List<int>())
                {
                    AppendUrl(sb, $"{baseUrl}/online-order?branchId={bId}", "0.9", null);
                }

                foreach (var p in products)
                {
                    AppendUrl(
                        sb,
                        $"{baseUrl}/online-order/product/{p.Id}?branchId={p.BranchId}",
                        "0.8",
                        p.LastModified);
                }
            }

            sb.AppendLine("</urlset>");
            return sb.ToString();
        }

        private static void AppendUrl(StringBuilder sb, string loc, string priority, DateTime? lastMod)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(loc)}</loc>");
            if (lastMod.HasValue)
            {
                sb.AppendLine($"    <lastmod>{lastMod.Value.ToUniversalTime():yyyy-MM-dd}</lastmod>");
            }

            sb.AppendLine("    <changefreq>daily</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        private sealed class ProductSitemapItem
        {
            public int Id { get; set; }
            public int BranchId { get; set; }
            public DateTime LastModified { get; set; }
        }
    }
}

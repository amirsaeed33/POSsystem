using System;
using System.IO;
using Abp.AspNetCore.Mvc.Controllers;
using Abp.Authorization;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using SmartPos.Orders;

namespace SmartPos.Web.Host.Controllers
{
    /// <summary>
    /// Serves hero banner images under /api so the Angular proxy can reach them
    /// without a dedicated static-file proxy entry.
    /// </summary>
    [Route("api/OnlineOrderHeroImages")]
    public class OnlineOrderHeroImageController : AbpController
    {
        [HttpGet("{*filePath}")]
        [AbpAllowAnonymous]
        [DontWrapResult]
        public IActionResult Get(string filePath)
        {
            var fullPath = OnlineOrderHeroImageStore.ResolveExistingFile(filePath);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return NotFound();
            }

            var contentType = GuessContentType(fullPath);
            return PhysicalFile(fullPath, contentType);
        }

        private static string GuessContentType(string fullPath)
        {
            var ext = Path.GetExtension(fullPath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                return "application/octet-stream";
            }

            ext = ext.ToLowerInvariant();
            if (ext == ".png")
            {
                return "image/png";
            }
            if (ext == ".gif")
            {
                return "image/gif";
            }
            if (ext == ".webp")
            {
                return "image/webp";
            }
            if (ext == ".svg")
            {
                return "image/svg+xml";
            }
            if (ext == ".jpg" || ext == ".jpeg")
            {
                return "image/jpeg";
            }

            return "application/octet-stream";
        }
    }
}

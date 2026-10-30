using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abp.UI;

namespace SmartPos.Orders
{
    public static class OnlineOrderHeroImageStore
    {
        private const string FolderName = "OnlineOrderHeroImages";

        public static bool IsNewImagePayload(string imageBase64)
        {
            return !string.IsNullOrWhiteSpace(imageBase64)
                   && imageBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase);
        }

        public static string SaveBase64Image(string imageBase64)
        {
            if (!IsNewImagePayload(imageBase64))
            {
                return null;
            }

            var parts = imageBase64.Split(',');
            if (parts.Length < 2)
            {
                throw new UserFriendlyException("Invalid hero image data.");
            }

            var meta = parts[0].ToLowerInvariant();
            var base64Data = parts[1].Trim();

            var extension = ".jpg";
            if (meta.Contains("png"))
            {
                extension = ".png";
            }
            else if (meta.Contains("gif"))
            {
                extension = ".gif";
            }
            else if (meta.Contains("webp"))
            {
                extension = ".webp";
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                throw new UserFriendlyException("Invalid hero image data.");
            }

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var roots = ResolveWebRoots();
            if (roots.Count == 0)
            {
                throw new UserFriendlyException("Could not resolve wwwroot for hero images.");
            }

            foreach (var root in roots)
            {
                var folder = Path.Combine(root, FolderName);
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, fileName), bytes);
            }

            return "/" + FolderName + "/" + fileName;
        }

        public static void DeleteIfExists(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            if (!imagePath.Contains(FolderName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Never delete shared seed defaults.
            if (imagePath.IndexOf("/defaults/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            var fullPath = ResolveExistingFile(imagePath);
            if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <summary>
        /// Resolves a stored hero image path (e.g. /OnlineOrderHeroImages/x.jpg or defaults/x.jpg)
        /// to an existing file on disk. Returns null when missing or unsafe.
        /// </summary>
        public static string ResolveExistingFile(string imagePathOrRelative)
        {
            if (string.IsNullOrWhiteSpace(imagePathOrRelative))
            {
                return null;
            }

            var normalized = imagePathOrRelative.Replace('\\', '/').Trim();
            // Strip leading folder if present.
            const string prefix = "/" + FolderName + "/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length);
            }
            else if (normalized.StartsWith(FolderName + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(FolderName.Length + 1);
            }

            normalized = normalized.TrimStart('/');
            if (string.IsNullOrWhiteSpace(normalized)
                || normalized.Contains("..", StringComparison.Ordinal)
                || Path.IsPathRooted(normalized))
            {
                return null;
            }

            var relativeUnderFolder = Path.Combine(
                FolderName,
                normalized.Replace('/', Path.DirectorySeparatorChar));

            foreach (var root in ResolveWebRoots())
            {
                var fullPath = Path.GetFullPath(Path.Combine(root, relativeUnderFolder));
                var rootFull = Path.GetFullPath(Path.Combine(root, FolderName));
                if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static List<string> ResolveWebRoots()
        {
            var candidates = new List<string>();
            var cwd = Directory.GetCurrentDirectory();
            var baseDir = AppContext.BaseDirectory;

            void Add(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                try
                {
                    var full = Path.GetFullPath(path);
                    if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
                    {
                        candidates.Add(full);
                    }
                }
                catch
                {
                    // ignore invalid paths
                }
            }

            Add(Path.Combine(cwd, "wwwroot"));
            Add(Path.Combine(baseDir, "wwwroot"));

            // bin/Debug/net8.0 -> project wwwroot
            Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "wwwroot")));

            var existing = candidates.Where(Directory.Exists).ToList();
            if (existing.Count > 0)
            {
                return existing;
            }

            // Create cwd wwwroot as last resort
            var fallback = Path.Combine(cwd, "wwwroot");
            Directory.CreateDirectory(fallback);
            return new List<string> { Path.GetFullPath(fallback) };
        }
    }
}

using System;
using System.IO;
using Abp.UI;

namespace SmartPos.Branches
{
    public static class BranchImageStore
    {
        private const string FolderName = "BranchImages";

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
                throw new UserFriendlyException("Invalid branch image data.");
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
                throw new UserFriendlyException("Invalid branch image data.");
            }

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", FolderName);
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            File.WriteAllBytes(Path.Combine(folder, fileName), bytes);

            return "/" + FolderName + "/" + fileName;
        }

        public static void DeleteIfExists(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}

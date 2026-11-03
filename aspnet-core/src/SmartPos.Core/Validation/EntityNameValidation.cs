using System.Linq;
using Abp.UI;

namespace SmartPos.Validation
{
    public static class EntityNameValidation
    {
        /// <summary>
        /// Requires trimmed length >= 2 and at least one letter (not symbols-only).
        /// </summary>
        public static string EnsureValid(string value, string entityLabel)
        {
            var name = value?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                throw new UserFriendlyException($"{entityLabel} name must be at least 2 characters.");
            }

            if (!name.Any(char.IsLetter))
            {
                throw new UserFriendlyException(
                    $"{entityLabel} name must contain at least one letter (not only special characters).");
            }

            return name;
        }
    }
}

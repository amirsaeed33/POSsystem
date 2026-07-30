using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPos.Emailing
{
    public static class EmailTemplateRenderer
    {
        public static string Render(string template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            if (values == null || values.Count == 0)
            {
                return template;
            }

            var result = template;
            foreach (var pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                var token = "{{" + pair.Key + "}}";
                result = ReplaceIgnoreCase(result, token, pair.Value ?? string.Empty);
            }

            return result;
        }

        private static string ReplaceIgnoreCase(string input, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
            {
                return input;
            }

            var comparison = StringComparison.OrdinalIgnoreCase;
            var sb = new StringBuilder();
            var previousIndex = 0;
            var index = input.IndexOf(oldValue, comparison);
            while (index >= 0)
            {
                sb.Append(input, previousIndex, index - previousIndex);
                sb.Append(newValue);
                previousIndex = index + oldValue.Length;
                index = input.IndexOf(oldValue, previousIndex, comparison);
            }

            sb.Append(input, previousIndex, input.Length - previousIndex);
            return sb.ToString();
        }
    }
}

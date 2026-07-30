using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Emailing.Dto
{
    public class PreviewEmailTemplateInput
    {
        [Required]
        public string Subject { get; set; }

        [Required]
        public string BodyHtml { get; set; }

        public Dictionary<string, string> SampleValues { get; set; }
    }
}

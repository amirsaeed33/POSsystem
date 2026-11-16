using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Orders;

namespace SmartPos.Orders.Dto
{
    public class OnlineOrderHeroSlideDto : EntityDto
    {
        public int BranchId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string Badge { get; set; }
        public string BadgeIcon { get; set; }
        public string TitleMain { get; set; }
        public string TitleHighlight { get; set; }
        public string TitleEnd { get; set; }
        public string Description { get; set; }
        public string OfferPill { get; set; }
        public string Rating { get; set; }
        public string PrimaryBtnText { get; set; }
        public string PrimaryTarget { get; set; }
        public string SecondaryBtnText { get; set; }
        public string SecondaryTarget { get; set; }
        public string ImagePath { get; set; }
        public string BgImagePath { get; set; }
        /// <summary>New upload payload (data:image...); ignored on read.</summary>
        public string ImageBase64 { get; set; }
        public string BgImageBase64 { get; set; }
    }

    public class CreateOrUpdateOnlineOrderHeroSlideDto
    {
        public int? Id { get; set; }

        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(OnlineOrderHeroSlide.MaxShortTextLength)]
        public string Badge { get; set; }

        [StringLength(16)]
        public string BadgeIcon { get; set; }

        [Required]
        [StringLength(OnlineOrderHeroSlide.MaxTitleLength)]
        public string TitleMain { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxTitleLength)]
        public string TitleHighlight { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxTitleLength)]
        public string TitleEnd { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxShortTextLength)]
        public string OfferPill { get; set; }

        [StringLength(32)]
        public string Rating { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxShortTextLength)]
        public string PrimaryBtnText { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxTargetLength)]
        public string PrimaryTarget { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxShortTextLength)]
        public string SecondaryBtnText { get; set; }

        [StringLength(OnlineOrderHeroSlide.MaxTargetLength)]
        public string SecondaryTarget { get; set; }

        public string ImageBase64 { get; set; }
        public string BgImageBase64 { get; set; }

        /// <summary>Keep existing path when no new upload.</summary>
        public string ImagePath { get; set; }
        public string BgImagePath { get; set; }
    }
}

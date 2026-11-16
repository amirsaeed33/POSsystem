using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Orders
{
    [Table("AppOnlineOrderHeroSlides")]
    public class OnlineOrderHeroSlide : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxShortTextLength = 128;
        public const int MaxTitleLength = 256;
        public const int MaxDescriptionLength = 1000;
        public const int MaxImagePathLength = 512;
        public const int MaxTargetLength = 32;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int SortOrder { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [StringLength(MaxShortTextLength)]
        public virtual string Badge { get; set; }

        [StringLength(16)]
        public virtual string BadgeIcon { get; set; }

        [StringLength(MaxTitleLength)]
        public virtual string TitleMain { get; set; }

        [StringLength(MaxTitleLength)]
        public virtual string TitleHighlight { get; set; }

        [StringLength(MaxTitleLength)]
        public virtual string TitleEnd { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        [StringLength(MaxShortTextLength)]
        public virtual string OfferPill { get; set; }

        [StringLength(32)]
        public virtual string Rating { get; set; }

        [StringLength(MaxShortTextLength)]
        public virtual string PrimaryBtnText { get; set; }

        [StringLength(MaxTargetLength)]
        public virtual string PrimaryTarget { get; set; }

        [StringLength(MaxShortTextLength)]
        public virtual string SecondaryBtnText { get; set; }

        [StringLength(MaxTargetLength)]
        public virtual string SecondaryTarget { get; set; }

        [StringLength(MaxImagePathLength)]
        public virtual string ImagePath { get; set; }

        [StringLength(MaxImagePathLength)]
        public virtual string BgImagePath { get; set; }
    }
}

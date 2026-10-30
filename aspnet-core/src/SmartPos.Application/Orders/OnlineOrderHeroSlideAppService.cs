using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Branches;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public class OnlineOrderHeroSlideAppService : SmartPosAppServiceBase, IOnlineOrderHeroSlideAppService
    {
        private readonly IRepository<OnlineOrderHeroSlide> _slideRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;

        public OnlineOrderHeroSlideAppService(
            IRepository<OnlineOrderHeroSlide> slideRepository,
            IRepository<Branch> branchRepository,
            IBranchAccessChecker branchAccessChecker)
        {
            _slideRepository = slideRepository;
            _branchRepository = branchRepository;
            _branchAccessChecker = branchAccessChecker;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Edit)]
        public async Task<ListResultDto<OnlineOrderHeroSlideDto>> GetByBranchAsync(int branchId)
        {
            if (branchId <= 0)
            {
                throw new UserFriendlyException("Branch is required.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var items = await _slideRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.BranchId == branchId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

                return new ListResultDto<OnlineOrderHeroSlideDto>(items.Select(MapToDto).ToList());
            }
        }

        [AbpAllowAnonymous]
        public async Task<ListResultDto<OnlineOrderHeroSlideDto>> GetActiveByBranchAsync(int branchId)
        {
            if (branchId <= 0)
            {
                return new ListResultDto<OnlineOrderHeroSlideDto>(new List<OnlineOrderHeroSlideDto>());
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branchExists = await _branchRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(b => b.Id == branchId && b.IsActive);

                if (!branchExists)
                {
                    return new ListResultDto<OnlineOrderHeroSlideDto>(new List<OnlineOrderHeroSlideDto>());
                }

                var items = await _slideRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.BranchId == branchId && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

                return new ListResultDto<OnlineOrderHeroSlideDto>(items.Select(MapToDto).ToList());
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Edit)]
        public async Task<OnlineOrderHeroSlideDto> CreateOrUpdateAsync(CreateOrUpdateOnlineOrderHeroSlideDto input)
        {
            if (input == null || input.BranchId <= 0)
            {
                throw new UserFriendlyException("Branch is required.");
            }

            if (string.IsNullOrWhiteSpace(input.TitleMain))
            {
                throw new UserFriendlyException("Title is required.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(x => x.Id == input.BranchId);
                if (branch == null)
                {
                    throw new UserFriendlyException("Branch was not found.");
                }

                OnlineOrderHeroSlide entity;
                if (input.Id.HasValue && input.Id.Value > 0)
                {
                    entity = await _slideRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.BranchId == input.BranchId);
                    if (entity == null)
                    {
                        throw new UserFriendlyException("Hero slide was not found.");
                    }
                }
                else
                {
                    entity = new OnlineOrderHeroSlide
                    {
                        TenantId = branch.TenantId,
                        BranchId = input.BranchId
                    };
                }

                entity.SortOrder = input.SortOrder;
                entity.IsActive = input.IsActive;
                entity.Badge = TrimOrNull(input.Badge, OnlineOrderHeroSlide.MaxShortTextLength);
                entity.BadgeIcon = TrimOrNull(input.BadgeIcon, 16);
                entity.TitleMain = input.TitleMain.Trim();
                entity.TitleHighlight = TrimOrNull(input.TitleHighlight, OnlineOrderHeroSlide.MaxTitleLength);
                entity.TitleEnd = TrimOrNull(input.TitleEnd, OnlineOrderHeroSlide.MaxTitleLength);
                entity.Description = TrimOrNull(input.Description, OnlineOrderHeroSlide.MaxDescriptionLength);
                entity.OfferPill = TrimOrNull(input.OfferPill, OnlineOrderHeroSlide.MaxShortTextLength);
                entity.Rating = TrimOrNull(input.Rating, 32);
                entity.PrimaryBtnText = TrimOrNull(input.PrimaryBtnText, OnlineOrderHeroSlide.MaxShortTextLength) ?? "Order Now";
                entity.PrimaryTarget = NormalizeTarget(input.PrimaryTarget);
                entity.SecondaryBtnText = TrimOrNull(input.SecondaryBtnText, OnlineOrderHeroSlide.MaxShortTextLength) ?? "Explore Menu";
                entity.SecondaryTarget = NormalizeTarget(input.SecondaryTarget);

                var uploadedCardImage = false;
                var uploadedBgImage = false;

                if (OnlineOrderHeroImageStore.IsNewImagePayload(input.ImageBase64))
                {
                    var old = entity.ImagePath;
                    entity.ImagePath = OnlineOrderHeroImageStore.SaveBase64Image(input.ImageBase64);
                    uploadedCardImage = !string.IsNullOrWhiteSpace(entity.ImagePath);
                    if (uploadedCardImage
                        && !string.Equals(old, entity.ImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        OnlineOrderHeroImageStore.DeleteIfExists(old);
                    }
                }
                else if (input.ImagePath != null)
                {
                    var old = entity.ImagePath;
                    entity.ImagePath = string.IsNullOrWhiteSpace(input.ImagePath)
                        ? null
                        : TrimOrNull(input.ImagePath, OnlineOrderHeroSlide.MaxImagePathLength);
                    if (string.IsNullOrWhiteSpace(entity.ImagePath)
                        && !string.Equals(old, entity.ImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        OnlineOrderHeroImageStore.DeleteIfExists(old);
                    }
                }

                if (OnlineOrderHeroImageStore.IsNewImagePayload(input.BgImageBase64))
                {
                    var old = entity.BgImagePath;
                    entity.BgImagePath = OnlineOrderHeroImageStore.SaveBase64Image(input.BgImageBase64);
                    uploadedBgImage = !string.IsNullOrWhiteSpace(entity.BgImagePath);
                    if (uploadedBgImage
                        && !string.Equals(old, entity.BgImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        OnlineOrderHeroImageStore.DeleteIfExists(old);
                    }
                }
                else if (input.BgImagePath != null)
                {
                    var old = entity.BgImagePath;
                    entity.BgImagePath = string.IsNullOrWhiteSpace(input.BgImagePath)
                        ? null
                        : TrimOrNull(input.BgImagePath, OnlineOrderHeroSlide.MaxImagePathLength);
                    if (string.IsNullOrWhiteSpace(entity.BgImagePath)
                        && !string.Equals(old, entity.BgImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        OnlineOrderHeroImageStore.DeleteIfExists(old);
                    }
                }

                // Card image and background must stay paired unless a dedicated bg was uploaded.
                if (uploadedCardImage && !uploadedBgImage && !string.IsNullOrWhiteSpace(entity.ImagePath))
                {
                    entity.BgImagePath = entity.ImagePath;
                }
                else if (string.IsNullOrWhiteSpace(entity.BgImagePath) && !string.IsNullOrWhiteSpace(entity.ImagePath))
                {
                    entity.BgImagePath = entity.ImagePath;
                }
                else if (string.IsNullOrWhiteSpace(entity.ImagePath) && !string.IsNullOrWhiteSpace(entity.BgImagePath))
                {
                    entity.ImagePath = entity.BgImagePath;
                }

                if (entity.Id == 0)
                {
                    await _slideRepository.InsertAsync(entity);
                }
                else
                {
                    await _slideRepository.UpdateAsync(entity);
                }

                await CurrentUnitOfWork.SaveChangesAsync();
                return MapToDto(entity);
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Edit)]
        public async Task DeleteAsync(EntityDto input)
        {
            if (input == null || input.Id <= 0)
            {
                throw new UserFriendlyException("Invalid slide.");
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var entity = await _slideRepository.FirstOrDefaultAsync(input.Id);
                if (entity == null)
                {
                    return;
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

                var imagePath = entity.ImagePath;
                var bgPath = entity.BgImagePath;
                await _slideRepository.DeleteAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();

                OnlineOrderHeroImageStore.DeleteIfExists(imagePath);
                if (!string.Equals(imagePath, bgPath, StringComparison.OrdinalIgnoreCase))
                {
                    OnlineOrderHeroImageStore.DeleteIfExists(bgPath);
                }
            }
        }

        private static string NormalizeTarget(string target)
        {
            var t = (target ?? string.Empty).Trim().ToLowerInvariant();
            return t == "deals" || t == "menu" || t == "home" ? t : "menu";
        }

        private static string TrimOrNull(string value, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLen ? trimmed : trimmed.Substring(0, maxLen);
        }

        private static OnlineOrderHeroSlideDto MapToDto(OnlineOrderHeroSlide entity)
        {
            return new OnlineOrderHeroSlideDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                Badge = entity.Badge,
                BadgeIcon = entity.BadgeIcon,
                TitleMain = entity.TitleMain,
                TitleHighlight = entity.TitleHighlight,
                TitleEnd = entity.TitleEnd,
                Description = entity.Description,
                OfferPill = entity.OfferPill,
                Rating = entity.Rating,
                PrimaryBtnText = entity.PrimaryBtnText,
                PrimaryTarget = entity.PrimaryTarget,
                SecondaryBtnText = entity.SecondaryBtnText,
                SecondaryTarget = entity.SecondaryTarget,
                ImagePath = entity.ImagePath,
                BgImagePath = entity.BgImagePath
            };
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Developers.Dto;

namespace SmartPos.Developers
{
    /// <summary>Host / super-admin only developer profile CRUD.</summary>
    [AbpAuthorize(PermissionNames.Pages_Developers)]
    public class DeveloperAppService :
        AsyncCrudAppService<Developer, DeveloperDto, int, PagedDeveloperResultRequestDto, CreateDeveloperDto, DeveloperDto>,
        IDeveloperAppService
    {
        public DeveloperAppService(IRepository<Developer> repository)
            : base(repository)
        {
            CreatePermissionName = PermissionNames.Pages_Developers_Create;
            UpdatePermissionName = PermissionNames.Pages_Developers_Edit;
            DeletePermissionName = PermissionNames.Pages_Developers_Delete;
            GetPermissionName = PermissionNames.Pages_Developers;
            GetAllPermissionName = PermissionNames.Pages_Developers;
        }

        protected override IQueryable<Developer> CreateFilteredQuery(PagedDeveloperResultRequestDto input)
        {
            return Repository.GetAll()
                .AsNoTracking()
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Email != null && x.Email.Contains(input.Keyword))
                         || (x.PhoneNumber != null && x.PhoneNumber.Contains(input.Keyword))
                         || (x.Tags != null && x.Tags.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        protected override IQueryable<Developer> ApplySorting(IQueryable<Developer> query, PagedDeveloperResultRequestDto input)
        {
            if (!input.Sorting.IsNullOrWhiteSpace())
            {
                return base.ApplySorting(query, input);
            }

            return query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name);
        }

        public override async Task<DeveloperDto> CreateAsync(CreateDeveloperDto input)
        {
            Normalize(input);
            ValidateRequired(input.Name);

            var entity = MapToEntity(input);

            if (DeveloperImageStore.IsNewImagePayload(input.ProfileImageBase64))
            {
                entity.ProfileImagePath = DeveloperImageStore.SaveBase64Image(input.ProfileImageBase64);
            }

            if (DeveloperImageStore.IsNewImagePayload(input.BackgroundImageBase64))
            {
                entity.BackgroundImagePath = DeveloperImageStore.SaveBase64Image(input.BackgroundImageBase64);
            }

            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(entity);
        }

        public override async Task<DeveloperDto> UpdateAsync(DeveloperDto input)
        {
            Normalize(input);
            ValidateRequired(input.Name);

            var entity = await Repository.GetAsync(input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Developer not found.");
            }

            var oldProfile = entity.ProfileImagePath;
            var oldBackground = entity.BackgroundImagePath;

            MapToEntity(input, entity);

            if (input.ClearProfileImage)
            {
                entity.ProfileImagePath = null;
                DeveloperImageStore.DeleteIfExists(oldProfile);
            }
            else if (DeveloperImageStore.IsNewImagePayload(input.ProfileImageBase64))
            {
                entity.ProfileImagePath = DeveloperImageStore.SaveBase64Image(input.ProfileImageBase64);
                if (!string.IsNullOrWhiteSpace(oldProfile) && oldProfile != entity.ProfileImagePath)
                {
                    DeveloperImageStore.DeleteIfExists(oldProfile);
                }
            }
            else
            {
                entity.ProfileImagePath = oldProfile;
            }

            if (input.ClearBackgroundImage)
            {
                entity.BackgroundImagePath = null;
                DeveloperImageStore.DeleteIfExists(oldBackground);
            }
            else if (DeveloperImageStore.IsNewImagePayload(input.BackgroundImageBase64))
            {
                entity.BackgroundImagePath = DeveloperImageStore.SaveBase64Image(input.BackgroundImageBase64);
                if (!string.IsNullOrWhiteSpace(oldBackground) && oldBackground != entity.BackgroundImagePath)
                {
                    DeveloperImageStore.DeleteIfExists(oldBackground);
                }
            }
            else
            {
                entity.BackgroundImagePath = oldBackground;
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(entity);
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            var entity = await Repository.FirstOrDefaultAsync(input.Id);
            if (entity == null)
            {
                return;
            }

            DeveloperImageStore.DeleteIfExists(entity.ProfileImagePath);
            DeveloperImageStore.DeleteIfExists(entity.BackgroundImagePath);
            await Repository.DeleteAsync(entity);
        }

        protected override void MapToEntity(DeveloperDto input, Developer entity)
        {
            entity.Name = input.Name;
            entity.Email = input.Email;
            entity.PhoneNumber = input.PhoneNumber;
            entity.Description = input.Description;
            entity.Education = input.Education;
            entity.Tags = input.Tags;
            entity.IsActive = input.IsActive;
            entity.SortOrder = input.SortOrder;
        }

        protected override Developer MapToEntity(CreateDeveloperDto createInput)
        {
            return new Developer
            {
                Name = createInput.Name,
                Email = createInput.Email,
                PhoneNumber = createInput.PhoneNumber,
                Description = createInput.Description,
                Education = createInput.Education,
                Tags = createInput.Tags,
                IsActive = createInput.IsActive,
                SortOrder = createInput.SortOrder,
            };
        }

        protected override DeveloperDto MapToEntityDto(Developer entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.ProfileImageBase64 = null;
            dto.BackgroundImageBase64 = null;
            dto.ClearProfileImage = false;
            dto.ClearBackgroundImage = false;
            return dto;
        }

        private static void ValidateRequired(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new UserFriendlyException("Name is required.");
            }
        }

        private static void Normalize(CreateDeveloperDto input)
        {
            input.Name = (input.Name ?? string.Empty).Trim();
            input.Email = (input.Email ?? string.Empty).Trim();
            input.PhoneNumber = (input.PhoneNumber ?? string.Empty).Trim();
            input.Description = (input.Description ?? string.Empty).Trim();
            input.Education = (input.Education ?? string.Empty).Trim();
            input.Tags = NormalizeTags(input.Tags);
        }

        private static void Normalize(DeveloperDto input)
        {
            input.Name = (input.Name ?? string.Empty).Trim();
            input.Email = (input.Email ?? string.Empty).Trim();
            input.PhoneNumber = (input.PhoneNumber ?? string.Empty).Trim();
            input.Description = (input.Description ?? string.Empty).Trim();
            input.Education = (input.Education ?? string.Empty).Trim();
            input.Tags = NormalizeTags(input.Tags);
        }

        private static string NormalizeTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return null;
            }

            var parts = tags
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct();
            var joined = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(joined) ? null : joined;
        }
    }
}

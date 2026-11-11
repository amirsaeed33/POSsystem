using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Sellers.Dto;

namespace SmartPos.Sellers
{
    /// <summary>Host review for reseller applications; anonymous register + public approved list.</summary>
    [AbpAuthorize(PermissionNames.Pages_Sellers)]
    public class SellerAppService : ApplicationService, ISellerAppService
    {
        private readonly IRepository<Seller> _sellerRepository;

        public SellerAppService(IRepository<Seller> sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        [AbpAllowAnonymous]
        public async Task<SellerDto> RegisterAsync(RegisterSellerDto input)
        {
            Normalize(input);
            ValidateRegister(input);

            var email = input.Email;
            var phone = input.PhoneNumber;

            var duplicate = await _sellerRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status != SellerStatus.Rejected)
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(email) && x.Email == email) ||
                    x.PhoneNumber == phone)
                .AnyAsync();

            if (duplicate)
            {
                throw new UserFriendlyException(
                    "An application with this phone or email is already pending or approved.");
            }

            var entity = new Seller
            {
                FullName = input.FullName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                PhoneNumber = phone,
                City = NullIfEmpty(input.City),
                CNIC = NullIfEmpty(input.CNIC),
                CompanyName = NullIfEmpty(input.CompanyName),
                ExperienceNotes = NullIfEmpty(input.ExperienceNotes),
                Message = NullIfEmpty(input.Message),
                Status = SellerStatus.Pending,
                IsPubliclyListed = false,
                CommissionPerSalePkr = Seller.DefaultCommissionPerSalePkr,
            };

            await _sellerRepository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            return MapDto(entity);
        }

        [AbpAllowAnonymous]
        public async Task<List<PublicSellerDto>> GetPublicSellersAsync()
        {
            var rows = await _sellerRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == SellerStatus.Approved && x.IsPubliclyListed)
                .OrderBy(x => x.FullName)
                .ToListAsync();

            return rows.Select(x => new PublicSellerDto
            {
                Id = x.Id,
                FullName = x.FullName,
                City = x.City,
                PhoneNumber = x.PhoneNumber,
                CompanyName = x.CompanyName,
                CommissionPerSalePkr = x.CommissionPerSalePkr,
            }).ToList();
        }

        public async Task<PagedResultDto<SellerDto>> GetAllAsync(PagedSellerResultRequestDto input)
        {
            var query = _sellerRepository.GetAll()
                .AsNoTracking()
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.FullName.Contains(input.Keyword)
                         || (x.Email != null && x.Email.Contains(input.Keyword))
                         || (x.PhoneNumber != null && x.PhoneNumber.Contains(input.Keyword))
                         || (x.City != null && x.City.Contains(input.Keyword))
                         || (x.CompanyName != null && x.CompanyName.Contains(input.Keyword)));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreationTime)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<SellerDto>(total, items.Select(MapDto).ToList());
        }

        public async Task<SellerDto> GetAsync(EntityDto<int> input)
        {
            var entity = await _sellerRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Seller application not found.");
            }

            return MapDto(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Sellers_Approve)]
        public async Task ApproveAsync(EntityDto<int> input)
        {
            var entity = await _sellerRepository.GetAsync(input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Seller application not found.");
            }

            if (entity.Status == SellerStatus.Approved && entity.IsPubliclyListed)
            {
                return;
            }

            entity.Status = SellerStatus.Approved;
            entity.IsPubliclyListed = true;
            entity.ReviewedTime = Clock.Now;
            entity.RejectionReason = null;
            if (entity.CommissionPerSalePkr <= 0)
            {
                entity.CommissionPerSalePkr = Seller.DefaultCommissionPerSalePkr;
            }

            await _sellerRepository.UpdateAsync(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Sellers_Approve)]
        public async Task RejectAsync(RejectSellerDto input)
        {
            if (input == null || input.Id <= 0)
            {
                throw new UserFriendlyException("Invalid seller application.");
            }

            var entity = await _sellerRepository.GetAsync(input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Seller application not found.");
            }

            entity.Status = SellerStatus.Rejected;
            entity.IsPubliclyListed = false;
            entity.ReviewedTime = Clock.Now;
            entity.RejectionReason = NullIfEmpty((input.RejectionReason ?? string.Empty).Trim());

            await _sellerRepository.UpdateAsync(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Sellers_Delete)]
        public async Task DeleteAsync(EntityDto<int> input)
        {
            var entity = await _sellerRepository.FirstOrDefaultAsync(input.Id);
            if (entity == null)
            {
                return;
            }

            await _sellerRepository.DeleteAsync(entity);
        }

        private static SellerDto MapDto(Seller entity)
        {
            return new SellerDto
            {
                Id = entity.Id,
                FullName = entity.FullName,
                Email = entity.Email,
                PhoneNumber = entity.PhoneNumber,
                City = entity.City,
                CNIC = entity.CNIC,
                CompanyName = entity.CompanyName,
                ExperienceNotes = entity.ExperienceNotes,
                Message = entity.Message,
                Status = entity.Status,
                StatusName = entity.Status.ToString(),
                IsPubliclyListed = entity.IsPubliclyListed,
                CommissionPerSalePkr = entity.CommissionPerSalePkr,
                CreationTime = entity.CreationTime,
                ReviewedTime = entity.ReviewedTime,
                ReviewNotes = entity.ReviewNotes,
                RejectionReason = entity.RejectionReason,
            };
        }

        private static void ValidateRegister(RegisterSellerDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FullName))
            {
                throw new UserFriendlyException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(input.PhoneNumber))
            {
                throw new UserFriendlyException("Phone number is required.");
            }
        }

        private static void Normalize(RegisterSellerDto input)
        {
            input.FullName = (input.FullName ?? string.Empty).Trim();
            input.Email = (input.Email ?? string.Empty).Trim();
            input.PhoneNumber = (input.PhoneNumber ?? string.Empty).Trim();
            input.City = (input.City ?? string.Empty).Trim();
            input.CNIC = (input.CNIC ?? string.Empty).Trim();
            input.CompanyName = (input.CompanyName ?? string.Empty).Trim();
            input.ExperienceNotes = (input.ExperienceNotes ?? string.Empty).Trim();
            input.Message = (input.Message ?? string.Empty).Trim();
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

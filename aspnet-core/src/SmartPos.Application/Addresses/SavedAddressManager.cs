using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using SmartPos.Orders;

namespace SmartPos.Addresses
{
    /// <summary>Shared upsert / default-address helpers for AppSavedAddresses.</summary>
    public class SavedAddressManager : Abp.Dependency.ITransientDependency
    {
        private readonly IRepository<SavedAddress> _repository;
        private readonly IRepository<DeliveryArea> _deliveryAreaRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SavedAddressManager(
            IRepository<SavedAddress> repository,
            IRepository<DeliveryArea> deliveryAreaRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _repository = repository;
            _deliveryAreaRepository = deliveryAreaRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task<string> GetDefaultAddressTextAsync(int ownerType, int ownerId)
        {
            if (ownerId <= 0)
            {
                return null;
            }

            return await _repository.GetAll()
                .AsNoTracking()
                .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .Select(a => a.Address)
                .FirstOrDefaultAsync();
        }

        public string GetDefaultAddressText(int ownerType, int ownerId)
        {
            if (ownerId <= 0)
            {
                return null;
            }

            return _repository.GetAll()
                .AsNoTracking()
                .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .Select(a => a.Address)
                .FirstOrDefault();
        }

        public async Task<SavedAddress> GetDefaultAsync(int ownerType, int ownerId)
        {
            if (ownerId <= 0)
            {
                return null;
            }

            return await _repository.GetAll()
                .AsNoTracking()
                .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .FirstOrDefaultAsync();
        }

        public IQueryable<SavedAddress> QueryForOwner(int ownerType, int ownerId)
        {
            return _repository.GetAll()
                .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId);
        }

        public IQueryable<SavedAddress> QueryAll()
        {
            return _repository.GetAll();
        }

        public async Task<SavedAddress> UpsertDefaultAsync(
            int ownerType,
            int ownerId,
            int? tenantId,
            string address,
            string landmark = null,
            int? deliveryAreaId = null,
            string deliveryAreaName = null,
            double? latitude = null,
            double? longitude = null,
            string label = null,
            int? existingId = null)
        {
            var trimmedAddress = (address ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedAddress) || ownerId <= 0)
            {
                return null;
            }

            var trimmedLandmark = string.IsNullOrWhiteSpace(landmark) ? null : landmark.Trim();
            var trimmedLabel = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
            var areaName = string.IsNullOrWhiteSpace(deliveryAreaName) ? null : deliveryAreaName.Trim();

            if (deliveryAreaId.HasValue && deliveryAreaId.Value > 0 && string.IsNullOrWhiteSpace(areaName))
            {
                var area = await _deliveryAreaRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == deliveryAreaId.Value);
                areaName = area?.Name;
            }

            var hasCoords = latitude.HasValue && longitude.HasValue
                && Math.Abs(latitude.Value) <= 90
                && Math.Abs(longitude.Value) <= 180;

            SavedAddress entity = null;
            if (existingId.HasValue && existingId.Value > 0)
            {
                entity = await _repository.FirstOrDefaultAsync(a =>
                    a.Id == existingId.Value &&
                    a.OwnerType == ownerType &&
                    a.OwnerId == ownerId);
            }

            if (entity == null)
            {
                var existing = await _repository.GetAll()
                    .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
                    .ToListAsync();

                entity = existing.FirstOrDefault(a =>
                    string.Equals((a.Address ?? string.Empty).Trim(), trimmedAddress, StringComparison.OrdinalIgnoreCase) &&
                    a.DeliveryAreaId == deliveryAreaId);
            }

            if (entity == null)
            {
                entity = new SavedAddress
                {
                    TenantId = tenantId,
                    OwnerType = ownerType,
                    OwnerId = ownerId,
                    Address = trimmedAddress,
                    Landmark = trimmedLandmark,
                    DeliveryAreaId = deliveryAreaId > 0 ? deliveryAreaId : null,
                    DeliveryAreaName = areaName,
                    Latitude = hasCoords ? latitude : null,
                    Longitude = hasCoords ? longitude : null,
                    Label = trimmedLabel,
                    IsDefault = true
                };
                await _repository.InsertAsync(entity);
            }
            else
            {
                entity.Address = trimmedAddress;
                entity.Landmark = trimmedLandmark ?? entity.Landmark;
                entity.DeliveryAreaId = deliveryAreaId > 0 ? deliveryAreaId : entity.DeliveryAreaId;
                entity.DeliveryAreaName = areaName ?? entity.DeliveryAreaName;
                if (hasCoords)
                {
                    entity.Latitude = latitude;
                    entity.Longitude = longitude;
                }
                if (!string.IsNullOrWhiteSpace(trimmedLabel))
                {
                    entity.Label = trimmedLabel;
                }
                entity.IsDefault = true;
            }

            var others = await _repository.GetAll()
                .Where(a =>
                    a.OwnerType == ownerType &&
                    a.OwnerId == ownerId &&
                    a.Id != entity.Id &&
                    a.IsDefault)
                .ToListAsync();
            foreach (var other in others)
            {
                other.IsDefault = false;
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            await _unitOfWorkManager.Current.SaveChangesAsync();
        }

        public Task<SavedAddress> GetAsync(int id)
        {
            return _repository.FirstOrDefaultAsync(id);
        }
    }
}

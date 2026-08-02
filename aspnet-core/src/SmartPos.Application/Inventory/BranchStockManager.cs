using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;

namespace SmartPos.Inventory
{
    public class BranchStockManager : DomainService, IBranchStockManager
    {
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IAbpSession _abpSession;

        public BranchStockManager(
            IRepository<BranchStock> branchStockRepository,
            IAbpSession abpSession)
        {
            _branchStockRepository = branchStockRepository;
            _abpSession = abpSession;
        }

        public async Task<decimal> GetQuantityAsync(int branchId, int productId)
        {
            var stock = await GetOrNullAsync(branchId, productId);
            return stock?.Quantity ?? 0;
        }

        public async Task<Dictionary<int, decimal>> GetQuantitiesAsync(int branchId, IEnumerable<int> productIds)
        {
            var ids = productIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var rows = await _branchStockRepository.GetAll()
                .Where(x => x.BranchId == branchId && ids.Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.Quantity })
                .ToListAsync();

            var result = ids.ToDictionary(id => id, _ => 0m);
            foreach (var row in rows)
            {
                result[row.ProductId] = row.Quantity;
            }

            return result;
        }

        public async Task<Dictionary<int, decimal>> GetAggregatedQuantitiesAsync(IEnumerable<int> productIds)
        {
            var ids = productIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var rows = await _branchStockRepository.GetAll()
                .Where(x => ids.Contains(x.ProductId))
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var result = ids.ToDictionary(id => id, _ => 0m);
            foreach (var row in rows)
            {
                result[row.ProductId] = row.Quantity;
            }

            return result;
        }

        public async Task IncreaseAsync(int branchId, int productId, decimal quantity, string productName = null)
        {
            if (quantity < 0)
            {
                throw new UserFriendlyException("Quantity must not be negative.");
            }

            if (quantity == 0)
            {
                return;
            }

            var stock = await GetOrCreateAsync(branchId, productId);
            stock.Quantity += quantity;
        }

        public async Task DecreaseAsync(int branchId, int productId, decimal quantity, string productName = null)
        {
            if (quantity < 0)
            {
                throw new UserFriendlyException("Quantity must not be negative.");
            }

            if (quantity == 0)
            {
                return;
            }

            var stock = await GetOrCreateAsync(branchId, productId);
            if (stock.Quantity < quantity)
            {
                var name = productName ?? $"Product #{productId}";
                throw new UserFriendlyException(
                    $"Insufficient stock for '{name}'. Available: {stock.Quantity}, requested: {quantity}.");
            }

            stock.Quantity -= quantity;
        }

        public async Task AdjustAsync(int branchId, int productId, decimal quantityChange, string productName = null)
        {
            var stock = await GetOrCreateAsync(branchId, productId);
            var newQty = stock.Quantity + quantityChange;
            if (newQty < 0)
            {
                var name = productName ?? $"Product #{productId}";
                throw new UserFriendlyException(
                    $"Adjustment would make stock negative for '{name}'. Available: {stock.Quantity}.");
            }

            stock.Quantity = newQty;
        }

        private async Task<BranchStock> GetOrNullAsync(int branchId, int productId)
        {
            return await _branchStockRepository.FirstOrDefaultAsync(
                x => x.BranchId == branchId && x.ProductId == productId);
        }

        private async Task<BranchStock> GetOrCreateAsync(int branchId, int productId)
        {
            var stock = await GetOrNullAsync(branchId, productId);
            if (stock != null)
            {
                return stock;
            }

            stock = new BranchStock
            {
                TenantId = _abpSession.TenantId,
                BranchId = branchId,
                ProductId = productId,
                Quantity = 0
            };
            await _branchStockRepository.InsertAsync(stock);
            await CurrentUnitOfWork.SaveChangesAsync();
            return stock;
        }
    }
}

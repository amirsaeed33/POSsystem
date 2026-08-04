using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    public class BranchStockManager : DomainService, IBranchStockManager
    {
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IAbpSession _abpSession;

        public BranchStockManager(
            IRepository<BranchStock> branchStockRepository,
            IRepository<Product> productRepository,
            IAbpSession abpSession)
        {
            _branchStockRepository = branchStockRepository;
            _productRepository = productRepository;
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
            if (!ids.Any())
            {
                return new Dictionary<int, decimal>();
            }

            var rows = await _branchStockRepository.GetAll()
                .Where(x => x.BranchId == branchId && ids.Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.Quantity })
                .ToListAsync();

            var map = ids.ToDictionary(id => id, _ => 0m);
            foreach (var row in rows)
            {
                map[row.ProductId] = row.Quantity;
            }

            return map;
        }

        public async Task IncreaseAsync(int branchId, int productId, decimal quantity)
        {
            if (quantity <= 0)
            {
                return;
            }

            var stock = await GetOrCreateAsync(branchId, productId);
            stock.Quantity += quantity;

            var product = await _productRepository.GetAsync(productId);
            product.StockQuantity += quantity;
        }

        public async Task DecreaseAsync(int branchId, int productId, decimal quantity, string productName = null)
        {
            if (quantity <= 0)
            {
                return;
            }

            var stock = await GetOrCreateAsync(branchId, productId);
            if (stock.Quantity < quantity)
            {
                var name = productName ?? (await _productRepository.GetAsync(productId)).Name;
                throw new UserFriendlyException(
                    $"Insufficient stock for '{name}'. Available: {stock.Quantity}, requested: {quantity}.");
            }

            stock.Quantity -= quantity;

            var product = await _productRepository.GetAsync(productId);
            product.StockQuantity -= quantity;
            if (product.StockQuantity < 0)
            {
                product.StockQuantity = 0;
            }
        }

        public async Task SetAsync(int branchId, int productId, decimal quantity)
        {
            var stock = await GetOrCreateAsync(branchId, productId);
            var delta = quantity - stock.Quantity;
            stock.Quantity = quantity;

            var product = await _productRepository.GetAsync(productId);
            product.StockQuantity += delta;
            if (product.StockQuantity < 0)
            {
                product.StockQuantity = 0;
            }
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

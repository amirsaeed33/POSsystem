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
            var info = await GetBranchProductInfoAsync(branchId, productIds);
            return info.ToDictionary(x => x.Key, x => x.Value.Quantity);
        }

        public async Task<Dictionary<int, BranchProductInfo>> GetBranchProductInfoAsync(
            int branchId,
            IEnumerable<int> productIds)
        {
            var ids = productIds?.Distinct().ToList() ?? new List<int>();
            if (!ids.Any())
            {
                return new Dictionary<int, BranchProductInfo>();
            }

            var rows = await _branchStockRepository.GetAll()
                .Where(x => x.BranchId == branchId && ids.Contains(x.ProductId))
                .Select(x => new
                {
                    x.ProductId,
                    x.Quantity,
                    x.Price,
                    x.WholesalePrice,
                    x.CostPrice
                })
                .ToListAsync();

            var products = await _productRepository.GetAll()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Price,
                    x.WholesalePrice,
                    x.CostPrice
                })
                .ToListAsync();
            var productMap = products.ToDictionary(x => x.Id);

            var map = new Dictionary<int, BranchProductInfo>();
            foreach (var id in ids)
            {
                productMap.TryGetValue(id, out var product);
                map[id] = new BranchProductInfo
                {
                    ProductId = id,
                    Quantity = 0,
                    Price = product?.Price ?? 0,
                    WholesalePrice = product?.WholesalePrice ?? 0,
                    CostPrice = product?.CostPrice ?? 0
                };
            }

            foreach (var row in rows)
            {
                map[row.ProductId] = new BranchProductInfo
                {
                    ProductId = row.ProductId,
                    Quantity = row.Quantity,
                    Price = row.Price,
                    WholesalePrice = row.WholesalePrice,
                    CostPrice = row.CostPrice
                };
            }

            return map;
        }

        public async Task<List<int>> GetAssignedBranchIdsAsync(int productId)
        {
            return await _branchStockRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .Select(x => x.BranchId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> HasAssignmentAsync(int branchId, int productId)
        {
            return await _branchStockRepository.GetAll()
                .AnyAsync(x => x.BranchId == branchId && x.ProductId == productId);
        }

        public async Task EnsureCanUseProductAtBranchAsync(int branchId, int productId)
        {
            var product = await _productRepository.GetAsync(productId);
            if (product.IsShared)
            {
                return;
            }

            if (!await HasAssignmentAsync(branchId, productId))
            {
                throw new UserFriendlyException(
                    $"Product '{product.Name}' is not assigned to this branch.");
            }
        }

        public async Task IncreaseAsync(int branchId, int productId, decimal quantity)
        {
            if (quantity <= 0)
            {
                return;
            }

            var stock = await GetOrCreateAsync(branchId, productId, allowCreateAssignment: false);
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

            var stock = await GetOrCreateAsync(branchId, productId, allowCreateAssignment: false);
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
            var stock = await GetOrCreateAsync(branchId, productId, allowCreateAssignment: false);
            var delta = quantity - stock.Quantity;
            stock.Quantity = quantity;

            var product = await _productRepository.GetAsync(productId);
            product.StockQuantity += delta;
            if (product.StockQuantity < 0)
            {
                product.StockQuantity = 0;
            }
        }

        public async Task SetPricesAsync(
            int branchId,
            int productId,
            decimal price,
            decimal wholesalePrice,
            decimal costPrice)
        {
            var stock = await GetOrCreateAsync(branchId, productId, allowCreateAssignment: true);
            stock.Price = price;
            stock.WholesalePrice = wholesalePrice;
            stock.CostPrice = costPrice;
        }

        public async Task UpsertStockAndPricesAsync(
            int branchId,
            int productId,
            decimal quantity,
            decimal price,
            decimal wholesalePrice,
            decimal costPrice)
        {
            var stock = await GetOrCreateAsync(branchId, productId, allowCreateAssignment: true);
            var delta = quantity - stock.Quantity;
            stock.Quantity = quantity;
            stock.Price = price;
            stock.WholesalePrice = wholesalePrice;
            stock.CostPrice = costPrice;

            var product = await _productRepository.GetAsync(productId);
            product.StockQuantity += delta;
            if (product.StockQuantity < 0)
            {
                product.StockQuantity = 0;
            }
        }

        public async Task RemoveAssignmentAsync(int branchId, int productId)
        {
            var stock = await GetOrNullAsync(branchId, productId);
            if (stock == null)
            {
                return;
            }

            if (stock.Quantity > 0)
            {
                throw new UserFriendlyException(
                    "Cannot remove branch assignment while stock quantity is greater than zero.");
            }

            await _branchStockRepository.DeleteAsync(stock);
        }

        private async Task<BranchStock> GetOrNullAsync(int branchId, int productId)
        {
            return await _branchStockRepository.FirstOrDefaultAsync(
                x => x.BranchId == branchId && x.ProductId == productId);
        }

        private async Task<BranchStock> GetOrCreateAsync(
            int branchId,
            int productId,
            bool allowCreateAssignment)
        {
            var stock = await GetOrNullAsync(branchId, productId);
            if (stock != null)
            {
                return stock;
            }

            var product = await _productRepository.GetAsync(productId);
            if (!product.IsShared && !allowCreateAssignment)
            {
                throw new UserFriendlyException(
                    $"Product '{product.Name}' is not assigned to this branch.");
            }

            stock = new BranchStock
            {
                TenantId = _abpSession.TenantId,
                BranchId = branchId,
                ProductId = productId,
                Quantity = 0,
                Price = product.Price,
                WholesalePrice = product.WholesalePrice,
                CostPrice = product.CostPrice
            };
            await _branchStockRepository.InsertAsync(stock);
            await CurrentUnitOfWork.SaveChangesAsync();
            return stock;
        }
    }
}

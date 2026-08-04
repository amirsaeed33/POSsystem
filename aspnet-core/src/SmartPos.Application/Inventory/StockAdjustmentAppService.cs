using System.Collections.Generic;
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
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Inventory.Dto;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    [AbpAuthorize(PermissionNames.Pages_StockAdjustments)]
    public class StockAdjustmentAppService : ApplicationService, IStockAdjustmentAppService
    {
        private readonly IRepository<StockAdjustment> _adjustmentRepository;
        private readonly IRepository<StockAdjustmentLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly IBranchStockManager _branchStockManager;

        public StockAdjustmentAppService(
            IRepository<StockAdjustment> adjustmentRepository,
            IRepository<StockAdjustmentLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            IBranchStockManager branchStockManager)
        {
            _adjustmentRepository = adjustmentRepository;
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _branchStockManager = branchStockManager;
        }

        public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto input)
        {
            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            if (input.AdjustmentDate == default)
            {
                input.AdjustmentDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            var adjustment = new StockAdjustment
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                AdjustmentDate = input.AdjustmentDate,
                Reason = input.Reason,
                Notes = input.Notes,
                Lines = new List<StockAdjustmentLine>()
            };

            foreach (var lineInput in input.Lines)
            {
                if (lineInput.QuantityChange == 0)
                {
                    throw new UserFriendlyException("Quantity change cannot be zero.");
                }

                var product = await _productRepository.GetAsync(lineInput.ProductId);
                var currentQty = await _branchStockManager.GetQuantityAsync(branchId, lineInput.ProductId);
                var newQty = currentQty + lineInput.QuantityChange;
                if (newQty < 0)
                {
                    throw new UserFriendlyException(
                        $"Adjustment would make stock negative for '{product.Name}'. Available: {currentQty}.");
                }

                await _branchStockManager.SetAsync(branchId, lineInput.ProductId, newQty);
                adjustment.Lines.Add(new StockAdjustmentLine
                {
                    ProductId = lineInput.ProductId,
                    QuantityChange = lineInput.QuantityChange
                });
            }

            await _adjustmentRepository.InsertAsync(adjustment);
            await CurrentUnitOfWork.SaveChangesAsync();
            adjustment.ReferenceNo = "ADJ-" + adjustment.Id.ToString("D6");
            await CurrentUnitOfWork.SaveChangesAsync();

            return await MapAsync(adjustment);
        }

        public async Task<StockAdjustmentDto> GetAsync(EntityDto<int> input)
        {
            var entity = await _adjustmentRepository.GetAllIncluding(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Stock adjustment not found.");
            }

            return await MapAsync(entity);
        }

        public async Task<PagedResultDto<StockAdjustmentDto>> GetAllAsync(PagedStockAdjustmentResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            var query = _adjustmentRepository.GetAllIncluding(x => x.Lines)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.ReferenceNo != null && x.ReferenceNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword)));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.Id)
                .PageBy(input)
                .ToListAsync();

            var dtos = new List<StockAdjustmentDto>();
            foreach (var item in items)
            {
                dtos.Add(await MapAsync(item));
            }

            return new PagedResultDto<StockAdjustmentDto>(total, dtos);
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var entity = await _adjustmentRepository.GetAllIncluding(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Stock adjustment not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

            foreach (var line in entity.Lines.ToList())
            {
                var product = await _productRepository.GetAsync(line.ProductId);
                var currentQty = await _branchStockManager.GetQuantityAsync(entity.BranchId, line.ProductId);
                var reversed = currentQty - line.QuantityChange;
                if (reversed < 0)
                {
                    throw new UserFriendlyException(
                        $"Cannot delete adjustment; reversing would make stock negative for '{product.Name}'.");
                }

                await _branchStockManager.SetAsync(entity.BranchId, line.ProductId, reversed);
                await _lineRepository.DeleteAsync(line);
            }

            await _adjustmentRepository.DeleteAsync(entity);
        }

        private async Task<StockAdjustmentDto> MapAsync(StockAdjustment entity)
        {
            var dto = new StockAdjustmentDto
            {
                Id = entity.Id,
                AdjustmentDate = entity.AdjustmentDate,
                Reason = entity.Reason,
                ReferenceNo = entity.ReferenceNo,
                Notes = entity.Notes,
                Lines = new List<StockAdjustmentLineDto>()
            };

            if (entity.Lines == null)
            {
                return dto;
            }

            foreach (var line in entity.Lines)
            {
                var product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
                dto.Lines.Add(new StockAdjustmentLineDto
                {
                    Id = line.Id,
                    StockAdjustmentId = line.StockAdjustmentId,
                    ProductId = line.ProductId,
                    ProductName = product?.Name,
                    QuantityChange = line.QuantityChange
                });
            }

            return dto;
        }
    }
}

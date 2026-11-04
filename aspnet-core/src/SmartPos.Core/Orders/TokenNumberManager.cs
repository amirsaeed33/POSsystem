using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using SmartPos.Restaurant;
using SmartPos.Sales;

namespace SmartPos.Orders
{
    public class TokenNumberManager : DomainService, ITokenNumberManager
    {
        private readonly IRepository<RestaurantOrder> _restaurantOrderRepository;
        private readonly IRepository<CustomerOrder> _customerOrderRepository;
        private readonly IRepository<Sale> _saleRepository;

        public TokenNumberManager(
            IRepository<RestaurantOrder> restaurantOrderRepository,
            IRepository<CustomerOrder> customerOrderRepository,
            IRepository<Sale> saleRepository)
        {
            _restaurantOrderRepository = restaurantOrderRepository;
            _customerOrderRepository = customerOrderRepository;
            _saleRepository = saleRepository;
        }

        public async Task<int> AllocateNextTokenNoAsync(int branchId)
        {
            // Resets to 0 at 12:00 AM (midnight) every day
            var today = Clock.Now.Date;
            var tomorrow = today.AddDays(1);

            int maxToken = 0;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var restaurantMax = await _restaurantOrderRepository.GetAll()
                    .AsNoTracking()
                    .Where(o => o.BranchId == branchId
                                && o.TokenNo.HasValue
                                && o.CreationTime >= today
                                && o.CreationTime < tomorrow)
                    .Select(o => o.TokenNo.Value)
                    .OrderByDescending(t => t)
                    .FirstOrDefaultAsync();

                var customerMax = await _customerOrderRepository.GetAll()
                    .AsNoTracking()
                    .Where(o => o.BranchId == branchId
                                && o.TokenNo.HasValue
                                && o.CreationTime >= today
                                && o.CreationTime < tomorrow)
                    .Select(o => o.TokenNo.Value)
                    .OrderByDescending(t => t)
                    .FirstOrDefaultAsync();

                var saleMax = await _saleRepository.GetAll()
                    .AsNoTracking()
                    .Where(s => s.BranchId == branchId
                                && s.TokenNo.HasValue
                                && s.CreationTime >= today
                                && s.CreationTime < tomorrow)
                    .Select(s => s.TokenNo.Value)
                    .OrderByDescending(t => t)
                    .FirstOrDefaultAsync();

                maxToken = Math.Max(restaurantMax, Math.Max(customerMax, saleMax));
            }

            return maxToken + 1;
        }
    }
}


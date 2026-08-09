using Microsoft.EntityFrameworkCore;
using Abp.Domain.Entities;
using Abp.Zero.EntityFrameworkCore;
using SmartPos.Accounts;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Brands;
using SmartPos.Categories;
using SmartPos.Customers;
using SmartPos.Emailing;
using SmartPos.Expenses;
using SmartPos.HostCatalog;
using SmartPos.Inventory;
using SmartPos.Lookups;
using SmartPos.MultiTenancy;
using SmartPos.Orders;
using SmartPos.Products;
using SmartPos.Purchases;
using SmartPos.Sales;
using SmartPos.Staffs;
using SmartPos.Suppliers;
using SmartPos.Units;

namespace SmartPos.EntityFrameworkCore
{
    public class SmartPosDbContext : AbpZeroDbContext<Tenant, Role, User, SmartPosDbContext>
    {
        public virtual DbSet<Category> Categories { get; set; }

        public virtual DbSet<Branch> Branches { get; set; }

        public virtual DbSet<BranchStock> BranchStocks { get; set; }

        public virtual DbSet<Brand> Brands { get; set; }

        public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

        public virtual DbSet<LookUp> LookUps { get; set; }

        public virtual DbSet<HostCatalogItem> HostCatalogItems { get; set; }

        public virtual DbSet<BranchSeedRequest> BranchSeedRequests { get; set; }

        public virtual DbSet<BranchSeedRequestItem> BranchSeedRequestItems { get; set; }

        public virtual DbSet<Unit> Units { get; set; }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Staff> Staff { get; set; }

        public virtual DbSet<StaffAttendance> StaffAttendances { get; set; }

        public virtual DbSet<StaffPayroll> StaffPayrolls { get; set; }

        public virtual DbSet<StaffHistory> StaffHistories { get; set; }

        public virtual DbSet<Supplier> Suppliers { get; set; }

        public virtual DbSet<BusinessAccount> Accounts { get; set; }

        public virtual DbSet<LedgerEntry> LedgerEntries { get; set; }

        public virtual DbSet<Purchase> Purchases { get; set; }

        public virtual DbSet<PurchaseLine> PurchaseLines { get; set; }

        public virtual DbSet<PurchaseReturn> PurchaseReturns { get; set; }

        public virtual DbSet<PurchaseReturnLine> PurchaseReturnLines { get; set; }

        public virtual DbSet<Sale> Sales { get; set; }

        public virtual DbSet<SaleLine> SaleLines { get; set; }

        public virtual DbSet<SaleReturn> SaleReturns { get; set; }

        public virtual DbSet<SaleReturnLine> SaleReturnLines { get; set; }

        public virtual DbSet<Expense> Expenses { get; set; }

        public virtual DbSet<CustomerOrder> CustomerOrders { get; set; }

        public virtual DbSet<CustomerOrderLine> CustomerOrderLines { get; set; }

        public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }

        public virtual DbSet<StockAdjustmentLine> StockAdjustmentLines { get; set; }

        public SmartPosDbContext(DbContextOptions<SmartPosDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// ABP only auto-sets <see cref="IMayHaveTenant.TenantId"/> for single-tenant apps,
        /// and skips entirely when <see cref="AbpDbContext.SuppressAutoSetTenantId"/> is true
        /// (can stay stuck on pooled contexts after seed). Always fill from UoW/session when missing.
        /// </summary>
        protected override void CheckAndSetMayHaveTenantIdProperty(object entityAsObj)
        {
            if (!(entityAsObj is IMayHaveTenant entity) || entity.TenantId != null)
            {
                return;
            }

            // Host seed / host requests: both null → leave null. Tenant requests: set from UoW or session.
            var tenantId = GetCurrentTenantIdOrNull() ?? AbpSession.TenantId;
            if (tenantId != null)
            {
                entity.TenantId = tenantId;
            }
        }

        /// <summary>
        /// Base requires UoW tenant == session tenant; that fails when the JWT tenant claim is missing
        /// but <c>Abp.TenantId</c> header still set the unit of work tenant — leaving CreatorUserId null.
        /// </summary>
        protected override long? GetAuditUserId()
        {
            return AbpSession.UserId ?? base.GetAuditUserId();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LookUp>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.Type, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            modelBuilder.Entity<HostCatalogItem>(b =>
            {
                b.HasIndex(x => new { x.Type, x.CompanyTypeId, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasIndex(x => new { x.Type, x.CompanyTypeId, x.IsActive });
                b.HasOne(x => x.CompanyType)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BranchSeedRequest>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.BranchId, x.Status });
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.CompanyType)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BranchSeedRequestItem>(b =>
            {
                b.HasIndex(x => new { x.BranchSeedRequestId, x.HostItemId })
                    .IsUnique();
                b.HasOne(x => x.BranchSeedRequest)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.BranchSeedRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.HostItem)
                    .WithMany()
                    .HasForeignKey(x => x.HostItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.BranchId, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Brand>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.BranchId, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Unit>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.BranchId, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.Property(x => x.Price).HasPrecision(18, 2);
                b.Property(x => x.WholesalePrice).HasPrecision(18, 2);
                b.Property(x => x.CostPrice).HasPrecision(18, 2);
                b.Property(x => x.StockQuantity).HasPrecision(18, 2);
                b.Property(x => x.AlertQuantityLimit).HasPrecision(18, 2);
                b.HasIndex(x => new { x.TenantId, x.Barcode })
                    .IsUnique()
                    .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");
            });

            modelBuilder.Entity<Branch>(b =>
            {
                b.Property(x => x.TaxPercent).HasPrecision(18, 2);
                b.Property(x => x.DiscountPercent).HasPrecision(18, 2);
                b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                b.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasIndex(x => x.StatusId);
                b.HasOne(x => x.StatusLookUp)
                    .WithMany()
                    .HasForeignKey(x => x.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BranchStock>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.Price).HasPrecision(18, 2);
                b.Property(x => x.WholesalePrice).HasPrecision(18, 2);
                b.Property(x => x.CostPrice).HasPrecision(18, 2);
                b.HasIndex(x => new { x.TenantId, x.BranchId, x.ProductId })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EmailTemplate>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            modelBuilder.Entity<BusinessAccount>(b =>
            {
                b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            });

            modelBuilder.Entity<LedgerEntry>(b =>
            {
                b.Property(x => x.Debit).HasPrecision(18, 2);
                b.Property(x => x.Credit).HasPrecision(18, 2);
                b.HasIndex(x => x.AccountId);
                b.HasIndex(x => x.TransactionDate);
                b.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Staff>(b =>
            {
                b.Property(x => x.BasicSalary).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasIndex(x => x.UserId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StaffAttendance>(b =>
            {
                b.Property(x => x.WorkingHours).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasIndex(x => new { x.StaffId, x.AttendanceDate });
                b.HasOne(x => x.Staff)
                    .WithMany(x => x.Attendances)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StaffPayroll>(b =>
            {
                b.Property(x => x.BasicSalary).HasPrecision(18, 2);
                b.Property(x => x.Allowance).HasPrecision(18, 2);
                b.Property(x => x.Bonus).HasPrecision(18, 2);
                b.Property(x => x.Deduction).HasPrecision(18, 2);
                b.Property(x => x.OvertimeAmount).HasPrecision(18, 2);
                b.Property(x => x.NetSalary).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasIndex(x => new { x.StaffId, x.Year, x.Month, x.BranchId })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                b.HasOne(x => x.Staff)
                    .WithMany(x => x.Payrolls)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StaffHistory>(b =>
            {
                b.HasIndex(x => x.StaffId);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Staff)
                    .WithMany()
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Supplier>(b =>
            {
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Purchase>(b =>
            {
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Supplier)
                    .WithMany()
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseLine>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.UnitCost).HasPrecision(18, 2);
                b.Property(x => x.LineTotal).HasPrecision(18, 2);
                b.HasOne(x => x.Purchase)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.PurchaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseReturn>(b =>
            {
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Purchase)
                    .WithMany()
                    .HasForeignKey(x => x.PurchaseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseReturnLine>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.UnitCost).HasPrecision(18, 2);
                b.Property(x => x.LineTotal).HasPrecision(18, 2);
                b.HasOne(x => x.PurchaseReturn)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.PurchaseReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.PurchaseLine)
                    .WithMany()
                    .HasForeignKey(x => x.PurchaseLineId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Sale>(b =>
            {
                b.Property(x => x.SubTotal).HasPrecision(18, 2);
                b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                b.Property(x => x.DiscountPercent).HasPrecision(18, 2);
                b.Property(x => x.TaxPercent).HasPrecision(18, 2);
                b.Property(x => x.TaxAmount).HasPrecision(18, 2);
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.Property(x => x.CashAmount).HasPrecision(18, 2);
                b.Property(x => x.CardAmount).HasPrecision(18, 2);
                b.Property(x => x.CreditAmount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SaleLine>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.UnitPrice).HasPrecision(18, 2);
                b.Property(x => x.LineTotal).HasPrecision(18, 2);
                b.HasOne(x => x.Sale)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SaleReturn>(b =>
            {
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Sale)
                    .WithMany()
                    .HasForeignKey(x => x.SaleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SaleReturnLine>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.UnitPrice).HasPrecision(18, 2);
                b.Property(x => x.LineTotal).HasPrecision(18, 2);
                b.HasOne(x => x.SaleReturn)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.SaleReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.SaleLine)
                    .WithMany()
                    .HasForeignKey(x => x.SaleLineId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Expense>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.PaymentAccount)
                    .WithMany()
                    .HasForeignKey(x => x.PaymentAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.ExpenseAccount)
                    .WithMany()
                    .HasForeignKey(x => x.ExpenseAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomerOrder>(b =>
            {
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Sale)
                    .WithMany()
                    .HasForeignKey(x => x.SaleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomerOrderLine>(b =>
            {
                b.Property(x => x.Quantity).HasPrecision(18, 2);
                b.Property(x => x.UnitPrice).HasPrecision(18, 2);
                b.Property(x => x.LineTotal).HasPrecision(18, 2);
                b.HasOne(x => x.Order)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockAdjustment>(b =>
            {
                b.HasIndex(x => x.ReferenceNo);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockAdjustmentLine>(b =>
            {
                b.Property(x => x.QuantityChange).HasPrecision(18, 2);
                b.HasOne(x => x.StockAdjustment)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.StockAdjustmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

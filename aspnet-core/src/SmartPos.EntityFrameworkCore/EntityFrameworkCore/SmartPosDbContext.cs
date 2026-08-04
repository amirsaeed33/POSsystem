using Microsoft.EntityFrameworkCore;
using Abp.Zero.EntityFrameworkCore;
using SmartPos.Accounts;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Brands;
using SmartPos.Categories;
using SmartPos.CompanyProfiles;
using SmartPos.Customers;
using SmartPos.Emailing;
using SmartPos.Expenses;
using SmartPos.Inventory;
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

        public virtual DbSet<CompanyProfile> CompanyProfiles { get; set; }

        public virtual DbSet<Unit> Units { get; set; }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Staff> Staff { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                b.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
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
                b.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Staff>(b =>
            {
                b.Property(x => x.BasicSalary).HasPrecision(18, 2);
                b.HasIndex(x => x.BranchId);
                b.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Supplier>(b =>
            {
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

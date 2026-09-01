using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace SmartPos.EntityFrameworkCore
{
    public static class SmartPosDbContextConfigurer
    {
        public static void Configure(DbContextOptionsBuilder<SmartPosDbContext> builder, string connectionString)
        {
            builder.UseSqlServer(connectionString, options =>
            {
                options.CommandTimeout(120);
            });
        }

        public static void Configure(DbContextOptionsBuilder<SmartPosDbContext> builder, DbConnection connection)
        {
            builder.UseSqlServer(connection, options =>
            {
                options.CommandTimeout(120);
            });
        }
    }
}

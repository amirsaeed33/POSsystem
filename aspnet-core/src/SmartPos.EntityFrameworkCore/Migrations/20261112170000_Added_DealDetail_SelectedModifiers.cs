using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261112170000_Added_DealDetail_SelectedModifiers")]
    public partial class Added_DealDetail_SelectedModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.AppDealDetails', N'SelectedModifiers') IS NULL
BEGIN
    ALTER TABLE [dbo].[AppDealDetails]
    ADD [SelectedModifiers] nvarchar(1024) NULL;
END

IF COL_LENGTH(N'dbo.AppDealDetails', N'SelectedOptionIds') IS NULL
BEGIN
    ALTER TABLE [dbo].[AppDealDetails]
    ADD [SelectedOptionIds] nvarchar(512) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.AppDealDetails', N'SelectedModifiers') IS NOT NULL
    ALTER TABLE [dbo].[AppDealDetails] DROP COLUMN [SelectedModifiers];

IF COL_LENGTH(N'dbo.AppDealDetails', N'SelectedOptionIds') IS NOT NULL
    ALTER TABLE [dbo].[AppDealDetails] DROP COLUMN [SelectedOptionIds];
");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Branch_StatusId_LookUpFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure host-scoped BranchStatus lookups exist before FK migrate.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM AppLookUps WHERE TenantId IS NULL AND [Type] = N'BranchStatus' AND [Name] = N'Pending' AND IsDeleted = 0)
    INSERT INTO AppLookUps (TenantId, [Type], [Name], DisplayName, SortOrder, IsActive, IsDeleted, CreationTime)
    VALUES (NULL, N'BranchStatus', N'Pending', N'Pending', 10, 1, 0, SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM AppLookUps WHERE TenantId IS NULL AND [Type] = N'BranchStatus' AND [Name] = N'Approved' AND IsDeleted = 0)
    INSERT INTO AppLookUps (TenantId, [Type], [Name], DisplayName, SortOrder, IsActive, IsDeleted, CreationTime)
    VALUES (NULL, N'BranchStatus', N'Approved', N'Approved', 20, 1, 0, SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM AppLookUps WHERE TenantId IS NULL AND [Type] = N'BranchStatus' AND [Name] = N'Rejected' AND IsDeleted = 0)
    INSERT INTO AppLookUps (TenantId, [Type], [Name], DisplayName, SortOrder, IsActive, IsDeleted, CreationTime)
    VALUES (NULL, N'BranchStatus', N'Rejected', N'Rejected', 30, 1, 0, SYSUTCDATETIME());
");

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "AppBranches",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE b
SET b.StatusId = l.Id
FROM AppBranches b
INNER JOIN AppLookUps l
    ON l.TenantId IS NULL
   AND l.[Type] = N'BranchStatus'
   AND l.IsDeleted = 0
   AND l.[Name] = b.[Status];

UPDATE b
SET b.StatusId = (
    SELECT TOP 1 Id FROM AppLookUps
    WHERE TenantId IS NULL AND [Type] = N'BranchStatus' AND [Name] = N'Approved' AND IsDeleted = 0
)
FROM AppBranches b
WHERE b.StatusId IS NULL;

UPDATE AppLookUps
SET IsDeleted = 1, DeletionTime = SYSUTCDATETIME()
WHERE [Type] = N'BranchStatus' AND TenantId IS NOT NULL AND IsDeleted = 0;
");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "AppBranches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppBranches");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_StatusId",
                table: "AppBranches",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppBranches_AppLookUps_StatusId",
                table: "AppBranches",
                column: "StatusId",
                principalTable: "AppLookUps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppBranches_AppLookUps_StatusId",
                table: "AppBranches");

            migrationBuilder.DropIndex(
                name: "IX_AppBranches_StatusId",
                table: "AppBranches");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AppBranches",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.Sql(@"
UPDATE b
SET b.[Status] = l.[Name]
FROM AppBranches b
INNER JOIN AppLookUps l ON l.Id = b.StatusId;
");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "AppBranches");
        }
    }
}

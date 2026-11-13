using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_UserPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AppUserPresences]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppUserPresences] (
        [Id] bigint NOT NULL IDENTITY,
        [TenantId] int NULL,
        [UserId] bigint NOT NULL,
        [BranchId] int NULL,
        [UserName] nvarchar(256) NULL,
        [FullName] nvarchar(256) NULL,
        [BranchName] nvarchar(256) NULL,
        [TenancyName] nvarchar(256) NULL,
        [RoleNames] nvarchar(256) NULL,
        [LoginTime] datetime2 NOT NULL,
        [LastSeen] datetime2 NOT NULL,
        [SignedOutTime] datetime2 NULL,
        CONSTRAINT [PK_AppUserPresences] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_AppUserPresences_UserId] ON [dbo].[AppUserPresences] ([UserId]);
    CREATE INDEX [IX_AppUserPresences_TenantId_BranchId_LastSeen] ON [dbo].[AppUserPresences] ([TenantId], [BranchId], [LastSeen]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AppUserPresences]', N'U') IS NOT NULL
    DROP TABLE [dbo].[AppUserPresences];
");
        }
    }
}

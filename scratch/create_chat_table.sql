IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppCustomerChatMessages')
BEGIN
    CREATE TABLE [dbo].[AppCustomerChatMessages](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [TenantId] [int] NULL,
        [BranchId] [int] NOT NULL,
        [OrderId] [int] NULL,
        [OrderNo] [nvarchar](64) NULL,
        [SessionId] [nvarchar](128) NULL,
        [SenderName] [nvarchar](256) NULL,
        [CustomerMobile] [nvarchar](64) NULL,
        [SenderType] [int] NOT NULL,
        [Message] [nvarchar](2000) NOT NULL,
        [IsReadByCustomer] [bit] NOT NULL DEFAULT(0),
        [IsReadByStore] [bit] NOT NULL DEFAULT(0),
        [CreationTime] [datetime2](7) NOT NULL,
        [CreatorUserId] [bigint] NULL,
        CONSTRAINT [PK_AppCustomerChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_AppCustomerChatMessages_BranchId] ON [dbo].[AppCustomerChatMessages]([BranchId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AppCustomerChatMessages_OrderId] ON [dbo].[AppCustomerChatMessages]([OrderId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AppCustomerChatMessages_OrderNo] ON [dbo].[AppCustomerChatMessages]([OrderNo] ASC);
    CREATE NONCLUSTERED INDEX [IX_AppCustomerChatMessages_SessionId] ON [dbo].[AppCustomerChatMessages]([SessionId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AppCustomerChatMessages_CreationTime] ON [dbo].[AppCustomerChatMessages]([CreationTime] ASC);
END

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261025110000_Added_CustomerChatMessages')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261025110000_Added_CustomerChatMessages', '8.0.8');
END

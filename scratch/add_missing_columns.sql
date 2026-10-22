IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsFreeDelivery')
BEGIN
    ALTER TABLE [AppProducts] ADD [IsFreeDelivery] bit NOT NULL CONSTRAINT [DF_AppProducts_IsFreeDelivery] DEFAULT 0;
    PRINT 'Added IsFreeDelivery to AppProducts';
END
ELSE
BEGIN
    PRINT 'IsFreeDelivery already exists in AppProducts';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'IsFreeDelivery')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [IsFreeDelivery] bit NOT NULL CONSTRAINT [DF_AppCustomerOrders_IsFreeDelivery] DEFAULT 0;
    PRINT 'Added IsFreeDelivery to AppCustomerOrders';
END
ELSE
BEGIN
    PRINT 'IsFreeDelivery already exists in AppCustomerOrders';
END

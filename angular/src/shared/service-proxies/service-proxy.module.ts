import { NgModule } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AbpHttpInterceptor } from 'abp-ng2-module';

import * as ApiServiceProxies from './service-proxies';
import { CustomerOrderServiceProxy } from './customer-order-service-proxy';
import { StockAdjustmentServiceProxy } from './stock-adjustment-service-proxy';

@NgModule({
    providers: [
        ApiServiceProxies.RoleServiceProxy,
        ApiServiceProxies.SessionServiceProxy,
        ApiServiceProxies.TenantServiceProxy,
        ApiServiceProxies.CategoryServiceProxy,
        ApiServiceProxies.BrandServiceProxy,
        ApiServiceProxies.UnitServiceProxy,
        ApiServiceProxies.ProductServiceProxy,
        ApiServiceProxies.CustomerServiceProxy,
        ApiServiceProxies.SupplierServiceProxy,
        ApiServiceProxies.BusinessAccountServiceProxy,
        ApiServiceProxies.LedgerEntryServiceProxy,
        ApiServiceProxies.PurchaseServiceProxy,
        ApiServiceProxies.PurchaseReturnServiceProxy,
        ApiServiceProxies.SaleServiceProxy,
        ApiServiceProxies.SaleReturnServiceProxy,
        CustomerOrderServiceProxy,
        StockAdjustmentServiceProxy,
        ApiServiceProxies.ExpenseServiceProxy,
        ApiServiceProxies.DashboardServiceProxy,
        ApiServiceProxies.ReportServiceProxy,
        ApiServiceProxies.UserServiceProxy,
        ApiServiceProxies.TokenAuthServiceProxy,
        ApiServiceProxies.AccountServiceProxy,
        ApiServiceProxies.ConfigurationServiceProxy,
        { provide: HTTP_INTERCEPTORS, useClass: AbpHttpInterceptor, multi: true }
    ]
})
export class ServiceProxyModule { }

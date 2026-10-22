import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateCustomerOrderDto,
    CustomerOrderDto,
    CustomerOrderLineDto,
    OverrideDeliveryFeeInput,
    PagedCustomerOrderResultRequestDto,
    PagedResultDto,
} from '../api/customer-order';
import { SaleDto } from '../api/sale';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CustomerOrderService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/CustomerOrder`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedCustomerOrderResultRequestDto
    ): Promise<PagedResultDto<CustomerOrderDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.status !== undefined) params.Status = input.status;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load customer orders');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load customer order'));
    }

    async create(input: CreateCustomerOrderDto): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                customerId: input.customerId,
                orderDate: this.toApiDate(input.orderDate),
                notes: input.notes,
                lines: input.lines,
            })
        );
        return this.map(this.unwrap(res, 'Failed to create customer order'));
    }

    async getOnlineCatalog(branchId?: number): Promise<any[]> {
        const params: any = {};
        if (branchId) params.branchId = branchId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineCatalog`, { params })
        );
        const result = this.unwrap(res, 'Failed to load catalog');
        return Array.isArray(result) ? result : result?.items || [];
    }

    async getOnlineProduct(id: number, branchId?: number): Promise<any> {
        const params: any = { id };
        if (branchId) params.branchId = branchId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineProduct`, { params })
        );
        return this.unwrap(res, 'Failed to load product details');
    }

    async getOnlineStoreHeader(branchId: number): Promise<any> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineStoreHeader`, { params: { branchId } })
        );
        return this.unwrap(res, 'Failed to load store info');
    }

    async createOnlineOrder(input: CreateCustomerOrderDto): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateOnlineOrder`, {
                customerId: input.customerId || 0,
                customerName: input.customerName,
                customerMobile: input.customerMobile,
                branchId: input.branchId,
                orderDate: this.toApiDate(input.orderDate || new Date()),
                notes: input.notes,
                latitude: input.latitude,
                longitude: input.longitude,
                googleMapsLink: input.googleMapsLink || (input as any).google_maps_link,
                google_maps_link: input.googleMapsLink || (input as any).google_maps_link,
                deliveryAddress: input.deliveryAddress,
                deliveryLandmark: input.deliveryLandmark,
                deliveryAreaId: input.deliveryAreaId,
                deliveryArea: input.deliveryArea,
                subtotal: input.subtotal,
                deliveryCharges: input.deliveryCharges,
                grandTotal: input.grandTotal,
                lines: input.lines,
            })
        );
        return this.map(this.unwrap(res, 'Failed to place online order'));
    }

    async trackOrder(orderNo: string): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/TrackOrder`, null, {
                params: { orderNo },
            })
        );
        return this.map(this.unwrap(res, 'Order not found'));
    }

    async overrideDeliveryFee(
        input: OverrideDeliveryFeeInput
    ): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/OverrideDeliveryFee`, {
                orderId: input.orderId,
                newDeliveryFee: input.newDeliveryFee,
                reason: input.reason,
            })
        );
        return this.map(this.unwrap(res, 'Failed to override delivery fee'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete customer order');
    }

    async approve(id: number): Promise<SaleDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Approve`, { id })
        );
        const sale = this.unwrap(res, 'Failed to approve order');
        return {
            id: sale.id ?? sale.Id,
            customerId: sale.customerId ?? sale.CustomerId,
            customerName: sale.customerName ?? sale.CustomerName,
            saleDate: sale.saleDate ?? sale.SaleDate,
            invoiceNo: sale.invoiceNo ?? sale.InvoiceNo,
            subTotal: sale.subTotal ?? sale.SubTotal ?? 0,
            discountAmount: sale.discountAmount ?? sale.DiscountAmount ?? 0,
            discountPercent: sale.discountPercent ?? sale.DiscountPercent ?? 0,
            taxPercent: sale.taxPercent ?? sale.TaxPercent ?? 0,
            taxAmount: sale.taxAmount ?? sale.TaxAmount ?? 0,
            totalAmount: sale.totalAmount ?? sale.TotalAmount ?? 0,
            paymentType: sale.paymentType ?? sale.PaymentType ?? 0,
            cashAmount: sale.cashAmount ?? sale.CashAmount ?? 0,
            cardAmount: sale.cardAmount ?? sale.CardAmount ?? 0,
            creditAmount: sale.creditAmount ?? sale.CreditAmount ?? 0,
            notes: sale.notes ?? sale.Notes,
            lines: [],
        };
    }

    async reject(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Reject`, { id })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to reject order');
    }

    async deliver(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Deliver`, { id })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to mark order as delivered');
    }

    private toApiDate(value?: string | Date): string | undefined {
        if (!value) return undefined;
        if (value instanceof Date) return value.toISOString();
        const parsed = new Date(
            value.includes('T') ? value : `${value}T00:00:00`
        );
        return isNaN(parsed.getTime()) ? value : parsed.toISOString();
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): CustomerOrderDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            customerId: item.customerId ?? item.CustomerId,
            customerName: item.customerName ?? item.CustomerName,
            orderDate: item.orderDate ?? item.OrderDate,
            orderNo: item.orderNo ?? item.OrderNo,
            status: item.status ?? item.Status ?? 0,
            statusName: item.statusName ?? item.StatusName,
            subTotal: item.subTotal ?? item.SubTotal ?? 0,
            deliveryFee: item.deliveryFee ?? item.DeliveryFee ?? 0,
            deliveryAreaId: item.deliveryAreaId ?? item.DeliveryAreaId,
            deliveryAreaName: item.deliveryAreaName ?? item.DeliveryAreaName,
            isFreeDelivery: item.isFreeDelivery ?? item.IsFreeDelivery ?? false,
            freeDeliveryReason: item.freeDeliveryReason ?? item.FreeDeliveryReason,
            originalDeliveryFee: item.originalDeliveryFee ?? item.OriginalDeliveryFee,
            deliveryFeeOverrideReason: item.deliveryFeeOverrideReason ?? item.DeliveryFeeOverrideReason,
            deliveryFeeOverriddenByName: item.deliveryFeeOverriddenByName ?? item.DeliveryFeeOverriddenByName,
            deliveryFeeOverrideTime: item.deliveryFeeOverrideTime ?? item.DeliveryFeeOverrideTime,
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            notes: item.notes ?? item.Notes,
            saleId: item.saleId ?? item.SaleId,
            saleInvoiceNo: item.saleInvoiceNo ?? item.SaleInvoiceNo,
            lines: (Array.isArray(lines) ? lines : []).map(
                (line: any): CustomerOrderLineDto => ({
                    id: line.id ?? line.Id,
                    orderId: line.orderId ?? line.OrderId,
                    productId: line.productId ?? line.ProductId,
                    productName: line.productName ?? line.ProductName,
                    quantity: line.quantity ?? line.Quantity ?? 0,
                    unitPrice: line.unitPrice ?? line.UnitPrice ?? 0,
                    lineTotal: line.lineTotal ?? line.LineTotal ?? 0,
                })
            ),
        };
    }
}

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    AssignRiderDto,
    CreateCustomerOrderDto,
    CustomerOrderDto,
    CustomerOrderLineDto,
    CustomerSavedAddressDto,
    GetCustomerSavedAddressesInput,
    RateCustomerOrderInput,
    CustomerOrderHistoryInput,
    DeliveryRiderStatusDto,
    OnlineCustomerProfileDto,
    OverrideDeliveryFeeInput,
    PagedCustomerOrderResultRequestDto,
    PagedResultDto,
    SaveCustomerAddressInput,
    UpsertOnlineCustomerProfileInput,
} from '../api/customer-order';
import { SaleDto } from '../api/sale';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({ providedIn: 'root' })
export class CustomerOrderService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/CustomerOrder`;
    private readonly catalogCache = new TtlCache<any[]>(2 * 60 * 1000);
    private readonly headerCache = new TtlCache<any>(5 * 60 * 1000);
    private readonly categoriesCache = new TtlCache<string[]>(5 * 60 * 1000);
    private readonly headerInflight = new Map<string, Promise<any>>();

    constructor(private http: HttpClient) {}

    clearCache(): void {
        this.catalogCache.clear();
        this.headerCache.clear();
        this.categoriesCache.clear();
        this.headerInflight.clear();
    }

    clearHeaderCache(branchId?: number): void {
        if (branchId == null) {
            this.headerCache.clear();
            this.headerInflight.clear();
            return;
        }
        const key = branchCacheKey(branchId, 'header');
        this.headerCache.delete(key);
        this.headerInflight.delete(key);
    }

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
                deliveryAddress: input.deliveryAddress,
                deliveryLandmark: input.deliveryLandmark,
                deliveryAreaId: input.deliveryAreaId,
                deliveryArea: input.deliveryArea,
                deliveryCharges: input.deliveryCharges,
                latitude: input.latitude,
                longitude: input.longitude,
                lines: input.lines,
            })
        );
        return this.map(this.unwrap(res, 'Failed to create customer order'));
    }

    async getOnlineCatalog(branchId?: number): Promise<any[]> {
        const key = branchCacheKey(branchId, 'catalog');
        const hit = this.catalogCache.get(key);
        if (hit) {
            return hit;
        }

        const params: any = {};
        if (branchId) params.branchId = branchId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineCatalog`, { params })
        );
        const result = this.unwrap(res, 'Failed to load catalog');
        const items = Array.isArray(result) ? result : result?.items || [];
        this.catalogCache.set(key, items);
        return items;
    }

    async getOnlineProduct(id: number, branchId?: number): Promise<any> {
        const params: any = { id };
        if (branchId) params.branchId = branchId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineProduct`, { params })
        );
        return this.unwrap(res, 'Failed to load product details');
    }

    async getOnlineBranches(): Promise<
        Array<{
            id: number;
            name: string;
            isRestaurant?: boolean;
            address?: string;
            imagePath?: string;
        }>
    > {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineBranches`)
        );
        const result = this.unwrap(res, 'Failed to load store branches');
        const items = Array.isArray(result) ? result : result?.items || [];
        return (Array.isArray(items) ? items : []).map((b: any) => ({
            id: b.id ?? b.Id,
            name: b.name ?? b.Name ?? `Branch #${b.id ?? b.Id}`,
            isRestaurant: !!(b.isRestaurant ?? b.IsRestaurant),
            address: b.address ?? b.Address ?? '',
            imagePath: (b.imagePath ?? b.ImagePath ?? '').toString().trim() || undefined,
        }));
    }

    async getOnlineStoreHeader(branchId: number): Promise<any> {
        const key = branchCacheKey(branchId, 'header');
        const hit = this.headerCache.get(key);
        if (hit) {
            return hit;
        }

        const inflight = this.headerInflight.get(key);
        if (inflight) {
            return inflight;
        }

        const request = (async () => {
            try {
                const res: any = await firstValueFrom(
                    this.http.get<any>(`${this.apiUrl}/GetOnlineStoreHeader`, {
                        params: { branchId },
                    })
                );
                const header = this.unwrap(res, 'Failed to load store info');
                this.headerCache.set(key, header);
                return header;
            } finally {
                this.headerInflight.delete(key);
            }
        })();

        this.headerInflight.set(key, request);
        return request;
    }

    async getOnlineCategories(branchId?: number): Promise<string[]> {
        const key = branchCacheKey(branchId, 'categories');
        const hit = this.categoriesCache.get(key);
        if (hit) {
            return hit;
        }

        const params: any = {};
        if (branchId) params.branchId = branchId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineCategories`, { params })
        );
        const result = this.unwrap(res, 'Failed to load categories');
        const items = Array.isArray(result) ? result : [];
        this.categoriesCache.set(key, items);
        return items;
    }

    async createOnlineOrder(input: CreateCustomerOrderDto): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateOnlineOrder`, {
                customerId: input.customerId || 0,
                customerName: input.customerName,
                customerMobile: input.customerMobile,
                branchId: input.branchId,
                // Server stamps OrderDate with Clock.Now (UTC) — avoid client local→ISO skew.
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
                paymentMethod: input.paymentMethod || 'CashOnDelivery',
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

    async rateOrder(input: RateCustomerOrderInput): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/RateOrder`, {
                orderNo: input.orderNo,
                rating: input.rating,
                comment: (input.comment || '').trim() || null,
            })
        );
        return this.map(this.unwrap(res, 'Failed to submit rating'));
    }

    async getOrdersByPhone(
        input: CustomerOrderHistoryInput
    ): Promise<CustomerOrderDto[]> {
        // ABP maps Get* actions to HTTP GET — query params, not POST body.
        const params: Record<string, string> = {
            Phone: (input.phone || '').trim(),
            MaxResultCount: String(input.maxResultCount ?? 30),
        };
        if (input.branchId) {
            params['BranchId'] = String(input.branchId);
        }
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOrdersByPhone`, { params })
        );
        const result = this.unwrap(res, 'Failed to load order history');
        const list = Array.isArray(result) ? result : result?.items || [];
        return list.map((item: any) => this.map(item));
    }

    async getBranchFeedbacks(
        branchId: number,
        maxResultCount = 20
    ): Promise<
        Array<{
            orderId: number;
            customerId?: number;
            orderNo?: string;
            customerName?: string;
            customerImagePath?: string | null;
            rating: number;
            orderDate: string | Date;
            branchName?: string;
            comment?: string;
            itemNames?: string[];
            itemsSummary?: string;
        }>
    > {
        if (!branchId || branchId <= 0) {
            return [];
        }
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetBranchFeedbacks`, {
                params: {
                    branchId: String(branchId),
                    maxResultCount: String(Math.min(maxResultCount || 5, 5)),
                },
            })
        );
        const result = this.unwrap(res, 'Failed to load branch feedbacks');
        const list = Array.isArray(result) ? result : result?.items || [];
        const mapped = (Array.isArray(list) ? list : []).map((item: any) => {
            const rawNames = item.itemNames ?? item.ItemNames;
            const itemNames = Array.isArray(rawNames)
                ? rawNames
                      .map((n: any) => String(n ?? '').trim())
                      .filter((n: string) => !!n)
                : [];
            const itemsSummary =
                (item.itemsSummary ?? item.ItemsSummary ?? '').trim() ||
                (itemNames.length ? itemNames.join(', ') : '');
            return {
                orderId: item.orderId ?? item.OrderId,
                customerId: Number(item.customerId ?? item.CustomerId ?? 0) || undefined,
                orderNo: item.orderNo ?? item.OrderNo,
                customerName: item.customerName ?? item.CustomerName ?? 'Customer',
                customerImagePath:
                    item.customerImagePath ?? item.CustomerImagePath ?? null,
                rating: Number(item.rating ?? item.Rating ?? 0),
                orderDate: this.asUtcDateValue(item.orderDate ?? item.OrderDate),
                branchName: item.branchName ?? item.BranchName,
                comment: item.comment ?? item.Comment ?? '',
                itemNames,
                itemsSummary,
            };
        });

        // Keep one card per customer (newest already preferred by API), max 5.
        const seen = new Set<number>();
        const distinct: typeof mapped = [];
        for (const row of mapped) {
            const key = row.customerId || row.orderId;
            if (seen.has(key)) {
                continue;
            }
            seen.add(key);
            distinct.push(row);
            if (distinct.length >= 5) {
                break;
            }
        }
        return distinct;
    }

    async getOnlineCustomerProfile(
        phone: string,
        branchId: number
    ): Promise<OnlineCustomerProfileDto | null> {
        const params: Record<string, string> = {
            phone: (phone || '').trim(),
            branchId: String(branchId),
        };
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnlineCustomerProfile`, { params })
        );
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || 'Failed to load customer profile'
            );
        }
        // ABP returns result: null when not found — do not fall back to the wrapper object.
        const result = res.result;
        if (result == null) return null;
        return this.mapProfile(result);
    }

    async upsertOnlineCustomerProfile(
        input: UpsertOnlineCustomerProfileInput
    ): Promise<OnlineCustomerProfileDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/UpsertOnlineCustomerProfile`, {
                branchId: input.branchId,
                name: input.name,
                phone: input.phone,
                email: input.email,
                address: input.address,
                latitude: input.latitude ?? null,
                longitude: input.longitude ?? null,
                imageBase64: input.imageBase64,
                clearImage: !!input.clearImage,
            })
        );
        return this.mapProfile(
            this.unwrap(res, 'Failed to save customer profile')
        );
    }

    private mapProfile(item: any): OnlineCustomerProfileDto {
        const rawAddresses =
            item.savedAddresses ?? item.SavedAddresses ?? [];
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            name: item.name ?? item.Name,
            phone: item.phone ?? item.Phone,
            email: item.email ?? item.Email,
            address: item.address ?? item.Address,
            latitude: item.latitude ?? item.Latitude ?? null,
            longitude: item.longitude ?? item.Longitude ?? null,
            imagePath: item.imagePath ?? item.ImagePath,
            savedAddresses: (Array.isArray(rawAddresses) ? rawAddresses : []).map(
                (a: any) => this.mapSavedAddress(a)
            ),
        };
    }

    async getSavedAddresses(
        input: GetCustomerSavedAddressesInput
    ): Promise<CustomerSavedAddressDto[]> {
        const params: Record<string, string> = {};
        if (input.customerId) params['CustomerId'] = String(input.customerId);
        if (input.branchId) params['BranchId'] = String(input.branchId);
        if (input.phone) params['Phone'] = input.phone.trim();
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetSavedAddresses`, { params })
        );
        const result = this.unwrap(res, 'Failed to load saved addresses');
        const items = Array.isArray(result) ? result : result?.items || result?.Items || [];
        return (Array.isArray(items) ? items : []).map((a: any) =>
            this.mapSavedAddress(a)
        );
    }

    async saveSavedAddress(
        input: SaveCustomerAddressInput
    ): Promise<CustomerSavedAddressDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SaveSavedAddress`, {
                id: input.id ?? null,
                customerId: input.customerId ?? null,
                branchId: input.branchId ?? null,
                phone: input.phone,
                label: input.label,
                address: input.address,
                landmark: input.landmark,
                deliveryAreaId: input.deliveryAreaId ?? null,
                deliveryAreaName: input.deliveryAreaName,
                latitude: input.latitude ?? null,
                longitude: input.longitude ?? null,
                isDefault: !!input.isDefault,
            })
        );
        return this.mapSavedAddress(
            this.unwrap(res, 'Failed to save address')
        );
    }

    async deleteSavedAddress(
        id: number,
        phone?: string,
        branchId?: number | null
    ): Promise<void> {
        const params: Record<string, string> = { Id: String(id) };
        if (phone) params['Phone'] = phone.trim();
        if (branchId) params['BranchId'] = String(branchId);
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/DeleteSavedAddress`, { params })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete address');
    }

    async getFavoriteProductIds(input: {
        phone?: string;
        branchId?: number | null;
        customerId?: number | null;
    }): Promise<number[]> {
        const params: Record<string, string> = {};
        if (input.customerId) params['CustomerId'] = String(input.customerId);
        if (input.branchId) params['BranchId'] = String(input.branchId);
        if (input.phone) params['Phone'] = input.phone.trim();
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetFavoriteProductIds`, { params })
        );
        const result = this.unwrap(res, 'Failed to load favorites');
        const items = Array.isArray(result) ? result : result?.items || result?.Items || [];
        return (Array.isArray(items) ? items : [])
            .map((id: any) => Number(id))
            .filter((id: number) => id > 0);
    }

    async toggleFavorite(input: {
        productId: number;
        phone?: string;
        branchId?: number | null;
        customerId?: number | null;
    }): Promise<{ productId: number; isFavorite: boolean }> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/ToggleFavorite`, {
                productId: input.productId,
                phone: input.phone || undefined,
                branchId: input.branchId ?? undefined,
                customerId: input.customerId ?? undefined,
            })
        );
        const result = this.unwrap(res, 'Failed to update favorite');
        return {
            productId: Number(result?.productId ?? result?.ProductId ?? input.productId),
            isFavorite: !!(result?.isFavorite ?? result?.IsFavorite),
        };
    }

    private mapSavedAddress(item: any): CustomerSavedAddressDto {
        return {
            id: item.id ?? item.Id,
            customerId: item.customerId ?? item.CustomerId,
            label: item.label ?? item.Label,
            address: item.address ?? item.Address,
            landmark: item.landmark ?? item.Landmark,
            deliveryAreaId: item.deliveryAreaId ?? item.DeliveryAreaId ?? null,
            deliveryAreaName: item.deliveryAreaName ?? item.DeliveryAreaName,
            latitude: item.latitude ?? item.Latitude ?? null,
            longitude: item.longitude ?? item.Longitude ?? null,
            isDefault: !!(item.isDefault ?? item.IsDefault),
        };
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

    /** POS Online: kitchen ticket + Approved, without auto rider (staff assigns next). */
    async prepareManualOnlineOrder(id: number): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/PrepareManualOnlineOrder`, { id })
        );
        return this.map(this.unwrap(res, 'Failed to prepare online order for kitchen'));
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

    /** Admin: mark Completed and insert payment/sale. */
    async complete(
        orderId: number,
        paymentReceivedAmount?: number | null
    ): Promise<void> {
        const body: any = { orderId };
        if (paymentReceivedAmount != null && paymentReceivedAmount !== undefined) {
            body.paymentReceivedAmount = paymentReceivedAmount;
        }
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Complete`, body)
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to complete order');
    }

    async getAvailableRiders(branchId?: number, currentOrderId?: number): Promise<DeliveryRiderStatusDto[]> {
        const params: any = {};
        if (branchId) params.branchId = branchId;
        if (currentOrderId) params.currentOrderId = currentOrderId;
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAvailableRiders`, { params })
        );
        const result = this.unwrap(res, 'Failed to load delivery riders');
        const list = Array.isArray(result) ? result : result?.items || [];
        return list.map((r: any) => ({
            riderId: r.riderId ?? r.RiderId,
            riderName: r.riderName ?? r.RiderName,
            riderPhone: r.riderPhone ?? r.RiderPhone,
            employeeCode: r.employeeCode ?? r.EmployeeCode,
            isBusy: Boolean(r.isBusy ?? r.IsBusy),
            busyOrderNo: r.busyOrderNo ?? r.BusyOrderNo,
            busyOrderId: r.busyOrderId ?? r.BusyOrderId,
            lastOrderTime: r.lastOrderTime ?? r.LastOrderTime,
            lastOrderTimeText: r.lastOrderTimeText ?? r.LastOrderTimeText,
            idleMinutes: r.idleMinutes ?? r.IdleMinutes ?? 0,
            todayDeliveredCount: r.todayDeliveredCount ?? r.TodayDeliveredCount ?? 0,
            todayDeliveredText: r.todayDeliveredText ?? r.TodayDeliveredText ?? 'No order delivered today',
        }));
    }

    async assignRider(input: AssignRiderDto): Promise<CustomerOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/AssignRider`, input)
        );
        return this.map(this.unwrap(res, 'Failed to assign rider'));
    }

    private toApiDate(value?: string | Date): string | undefined {
        if (!value) return undefined;
        if (value instanceof Date) return value.toISOString();
        const parsed = new Date(
            value.includes('T') ? value : `${value}T00:00:00`
        );
        return isNaN(parsed.getTime()) ? value : parsed.toISOString();
    }

    /**
     * API/SQL DateTimes are UTC wall-clock with Kind=Unspecified (no Z).
     * Without a Z, JS/Angular treat them as local and show ~5h off in PKT.
     */
    private asUtcDateValue(value: any): any {
        if (value == null || value === '') {
            return value;
        }
        if (value instanceof Date) {
            return value;
        }
        const s = String(value).trim();
        if (!s) {
            return value;
        }
        if (/([zZ]|[+-]\d{2}:\d{2})$/.test(s)) {
            return s;
        }
        // Keep date-only values date-only (avoid shifting the calendar day).
        if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
            return s;
        }
        return s.includes('T') ? `${s}Z` : `${s}T00:00:00Z`;
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
            customerPhone: item.customerPhone ?? item.CustomerPhone,
            customerAddress: item.customerAddress ?? item.CustomerAddress,
            orderDate: this.asUtcDateValue(item.orderDate ?? item.OrderDate),
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
            deliveryFeeOverrideTime: this.asUtcDateValue(
                item.deliveryFeeOverrideTime ?? item.DeliveryFeeOverrideTime
            ),
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            notes: item.notes ?? item.Notes,
            paymentMethod: item.paymentMethod ?? item.PaymentMethod,
            deliveryRating: item.deliveryRating ?? item.DeliveryRating ?? null,
            customerRating: item.customerRating ?? item.CustomerRating ?? null,
            customerFeedbackComment:
                item.customerFeedbackComment ?? item.CustomerFeedbackComment ?? null,
            saleId: item.saleId ?? item.SaleId,
            saleInvoiceNo: item.saleInvoiceNo ?? item.SaleInvoiceNo,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            riderId: item.riderId ?? item.RiderId,
            riderName: item.riderName ?? item.RiderName,
            riderPhone: item.riderPhone ?? item.RiderPhone,
            riderAssignedTime: this.asUtcDateValue(
                item.riderAssignedTime ?? item.RiderAssignedTime
            ),
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

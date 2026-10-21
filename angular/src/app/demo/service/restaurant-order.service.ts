import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateOrUpdateRestaurantOrderDto,
    RestaurantOrderDto,
    SendToKitchenResultDto,
    SettleRestaurantOrderDto,
    SplitBillDto,
} from '../api/restaurant-order';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RestaurantOrderService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/RestaurantOrder`;

    constructor(private http: HttpClient) {}

    async getActiveOrders(branchId: number): Promise<RestaurantOrderDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetActiveOrders`, {
                params: { branchId },
            })
        );
        const list = this.unwrap(res, 'Failed to load active orders');
        return Array.isArray(list) ? list : [];
    }

    async get(id: number): Promise<RestaurantOrderDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.unwrap(res, 'Failed to load order');
    }

    async getByTable(tableId: number): Promise<RestaurantOrderDto | null> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetByTable`, {
                params: { tableId },
            })
        );
        return this.unwrap(res, 'Failed to load table order');
    }

    async saveOrder(
        dto: CreateOrUpdateRestaurantOrderDto
    ): Promise<RestaurantOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SaveOrder`, dto)
        );
        return this.unwrap(res, 'Failed to save restaurant order');
    }

    async sendToKitchen(orderId: number): Promise<SendToKitchenResultDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SendToKitchen`, null, {
                params: { orderId },
            })
        );
        return this.unwrap(res, 'Failed to send order to kitchen');
    }

    async printBill(orderId: number): Promise<RestaurantOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/PrintBill`, null, {
                params: { orderId },
            })
        );
        return this.unwrap(res, 'Failed to mark order as billed');
    }

    async settleOrder(dto: SettleRestaurantOrderDto): Promise<number> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SettleOrder`, dto)
        );
        return this.unwrap(res, 'Failed to settle restaurant order');
    }

    async cancelOrder(orderId: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CancelOrder`, null, {
                params: { orderId },
            })
        );
        this.unwrap(res, 'Failed to cancel order');
    }

    async splitBill(dto: SplitBillDto): Promise<RestaurantOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SplitBill`, dto)
        );
        return this.unwrap(res, 'Failed to split bill');
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }
}

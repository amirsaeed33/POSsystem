import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    PagedRiderOrderResultDto,
    RejectRiderOrderDto,
    RiderDashboardStatsDto,
    RiderOrderDto,
    RiderOrderHistoryInput,
} from '../api/rider';

@Injectable({
    providedIn: 'root',
})
export class RiderService {
    private readonly baseUrl = `${environment.apiUrl}/api/services/app/RiderOrder`;

    constructor(private http: HttpClient) {}

    async getDashboardStats(): Promise<RiderDashboardStatsDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.baseUrl}/GetDashboardStats`)
        );
        return res?.result ?? res;
    }

    async getAssignedOrders(): Promise<RiderOrderDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.baseUrl}/GetAssignedOrders`)
        );
        return res?.result ?? res ?? [];
    }

    async getOrderHistory(
        input: RiderOrderHistoryInput
    ): Promise<PagedRiderOrderResultDto> {
        let params = new HttpParams();
        if (input.fromDate) {
            params = params.set('FromDate', input.fromDate);
        }
        if (input.toDate) {
            params = params.set('ToDate', input.toDate);
        }
        if (input.keyword && input.keyword.trim()) {
            params = params.set('Keyword', input.keyword.trim());
        }
        if (input.status !== undefined && input.status !== null) {
            params = params.set('Status', input.status.toString());
        }
        if (input.skipCount !== undefined) {
            params = params.set('SkipCount', input.skipCount.toString());
        }
        if (input.maxResultCount !== undefined) {
            params = params.set(
                'MaxResultCount',
                input.maxResultCount.toString()
            );
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.baseUrl}/GetOrderHistory`, { params })
        );
        const data = res?.result ?? res;
        return {
            totalCount: data?.totalCount ?? 0,
            items: data?.items ?? [],
        };
    }

    async acceptOrder(orderId: number): Promise<RiderOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.baseUrl}/AcceptOrder`, { id: orderId })
        );
        return res?.result ?? res;
    }

    async rejectOrder(input: RejectRiderOrderDto): Promise<RiderOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.baseUrl}/RejectOrder`, input)
        );
        return res?.result ?? res;
    }

    async completeDelivery(orderId: number): Promise<RiderOrderDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.baseUrl}/CompleteDelivery`, {
                id: orderId,
            })
        );
        return res?.result ?? res;
    }
}

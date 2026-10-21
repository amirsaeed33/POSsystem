import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    ChangeTableStatusDto,
    CreateRestaurantTableDto,
    RestaurantTableDto,
} from '../api/restaurant-table';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RestaurantTableService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/RestaurantTable`;

    constructor(private http: HttpClient) {}

    async getTables(branchId: number): Promise<RestaurantTableDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetTables`, {
                params: { branchId },
            })
        );
        const list = this.unwrap(res, 'Failed to load restaurant tables');
        return (Array.isArray(list) ? list : []).map((t) => this.map(t));
    }

    async get(id: number): Promise<RestaurantTableDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load table'));
    }

    async create(dto: CreateRestaurantTableDto): Promise<RestaurantTableDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, dto)
        );
        return this.map(this.unwrap(res, 'Failed to create table'));
    }

    async update(dto: RestaurantTableDto): Promise<RestaurantTableDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, dto)
        );
        return this.map(this.unwrap(res, 'Failed to update table'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        this.unwrap(res, 'Failed to delete table');
    }

    async changeStatus(dto: ChangeTableStatusDto): Promise<RestaurantTableDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/ChangeStatus`, dto)
        );
        return this.map(this.unwrap(res, 'Failed to update table status'));
    }

    async transferTable(fromTableId: number, toTableId: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/TransferTable`, null, {
                params: { fromTableId, toTableId },
            })
        );
        this.unwrap(res, 'Failed to transfer table');
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): RestaurantTableDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            tableNo: item.tableNo ?? item.TableNo,
            area: item.area ?? item.Area ?? 'Main Hall',
            capacity: item.capacity ?? item.Capacity ?? 4,
            status: item.status ?? item.Status ?? 0,
            statusName: item.statusName ?? item.StatusName ?? 'Available',
            currentOrderId: item.currentOrderId ?? item.CurrentOrderId ?? null,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}

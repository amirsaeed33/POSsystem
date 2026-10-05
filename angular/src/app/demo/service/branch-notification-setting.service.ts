import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    BranchNotificationSettingDto,
    CreateOrUpdateBranchNotificationSettingDto
} from '../api/branch-notification-setting';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class BranchNotificationSettingService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/BranchNotificationSetting`;

    constructor(private http: HttpClient) {}

    async get(branchId: number): Promise<BranchNotificationSettingDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { branchId: branchId.toString() } })
        );
        const result = this.unwrap(res, 'Failed to load notification settings');
        return this.map(result);
    }

    async createOrUpdate(input: CreateOrUpdateBranchNotificationSettingDto): Promise<BranchNotificationSettingDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateOrUpdate`, input)
        );
        const result = this.unwrap(res, 'Failed to save notification settings');
        return this.map(result);
    }

    async delete(branchId: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, { params: { branchId: branchId.toString() } })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete notification settings');
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }
        return res.result !== undefined ? res.result : res;
    }

    private map(item: any): BranchNotificationSettingDto {
        const emails = item.emails ?? item.Emails ?? '';
        let emailList: string[] = item.emailList ?? item.EmailList ?? [];
        if (!emailList || !emailList.length) {
            emailList = emails
                ? emails.split(',').map((e: string) => e.trim()).filter((e: string) => !!e)
                : [];
        }

        return {
            id: item.id ?? item.Id ?? 0,
            tenantId: item.tenantId ?? item.TenantId ?? null,
            branchId: item.branchId ?? item.BranchId ?? 0,
            isEnabled: !!(item.isEnabled ?? item.IsEnabled),
            notifyOnSale: !!(item.notifyOnSale ?? item.NotifyOnSale),
            notifyOnPurchase: !!(item.notifyOnPurchase ?? item.NotifyOnPurchase),
            notifyOnExpense: !!(item.notifyOnExpense ?? item.NotifyOnExpense),
            emails: emails,
            emailList: emailList
        };
    }
}

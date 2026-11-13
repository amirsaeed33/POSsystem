import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface PaymentMethodDto {
    id: number;
    kind: number;
    kindName: string;
    accountTitle: string;
    accountNumber: string;
    bankName?: string;
    instructions?: string;
    isActive: boolean;
    hasQr: boolean;
    qrBase64?: string;
    qrContentType?: string;
}

export interface AssignablePlanDto {
    id: number;
    name: string;
    monthlyAmount: number;
    yearlyAmount: number;
}

export interface TenantSubscriptionDto {
    id?: number | null;
    tenantId: number;
    tenancyName?: string;
    tenantName?: string;
    packageId?: number | null;
    planName?: string;
    cycle: number;
    cycleName?: string;
    amount: number;
    paidUntil?: string | null;
    daysLeft: number;
    status: string;
    canPay: boolean;
    hasPendingPayment: boolean;
}

export interface SubscriptionPaymentDto {
    id: number;
    tenantId: number;
    tenancyName?: string;
    tenantName?: string;
    planName?: string;
    cycle: number;
    cycleName?: string;
    amount: number;
    method: number;
    methodName?: string;
    status: number;
    statusName?: string;
    reviewNote?: string;
    creationTime?: string;
    reviewedTime?: string | null;
    hasReceipt: boolean;
}

export interface MySubscriptionDto {
    subscription: TenantSubscriptionDto;
    paymentMethods: PaymentMethodDto[];
    payments: SubscriptionPaymentDto[];
    availablePlans: AssignablePlanDto[];
}

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/SubscriptionBilling`;
    private accessCache: { at: number; value: Awaited<ReturnType<SubscriptionService['readAccessStatus']>> } | null = null;

    constructor(private http: HttpClient) {}

    getPaymentMethods(): Promise<PaymentMethodDto[]> {
        return this.getList('/GetPaymentMethods', 'Failed to load payment methods', (x) => this.mapMethod(x));
    }

    savePaymentMethod(input: {
        id?: number | null;
        kind: number;
        accountTitle: string;
        accountNumber: string;
        bankName?: string;
        instructions?: string;
        isActive: boolean;
        qrBase64?: string | null;
        qrContentType?: string | null;
        clearQr?: boolean;
    }): Promise<PaymentMethodDto> {
        return this.post('/SavePaymentMethod', {
            id: input.id ?? undefined,
            kind: input.kind,
            accountTitle: input.accountTitle,
            accountNumber: input.accountNumber,
            bankName: input.bankName || null,
            instructions: input.instructions || null,
            isActive: input.isActive,
            qrBase64: input.qrBase64 || null,
            qrContentType: input.qrContentType || null,
            clearQr: !!input.clearQr,
        }, 'Failed to save payment method').then((x) => this.mapMethod(x));
    }

    deletePaymentMethod(id: number): Promise<void> {
        return firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/DeletePaymentMethod`, { params: { Id: id } })
        ).then((res) => {
            if (res != null) {
                this.unwrap(res, 'Failed to delete payment method');
            }
        });
    }

    getAssignablePlans(): Promise<AssignablePlanDto[]> {
        return this.getList('/GetAssignablePlans', 'Failed to load plans', (x) => this.mapPlan(x));
    }

    getSubscriptions(): Promise<TenantSubscriptionDto[]> {
        return this.getList('/GetSubscriptions', 'Failed to load subscriptions', (x) => this.mapSubscription(x));
    }

    getTenantSubscription(tenantId: number): Promise<TenantSubscriptionDto> {
        return firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetTenantSubscription`, { params: { tenantId } })
        ).then((res) => this.mapSubscription(this.unwrap(res, 'Failed to load the plan')));
    }

    saveTenantSubscription(input: {
        tenantId: number;
        packageId: number;
        cycle: number;
        amount: number;
        paidUntil?: string | null;
        clearPaidUntil?: boolean;
    }): Promise<TenantSubscriptionDto> {
        return this.post('/SaveTenantSubscription', input, 'Failed to save the plan')
            .then((x) => this.mapSubscription(x));
    }

    getPendingPayments(): Promise<SubscriptionPaymentDto[]> {
        return this.getList('/GetPendingPayments', 'Failed to load the waiting list', (x) => this.mapPayment(x));
    }

    getReceipt(id: number): Promise<{ fileName: string; contentType: string; base64: string }> {
        return firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetReceipt`, { params: { id } })
        ).then((res) => {
            const raw = this.unwrap(res, 'Failed to open the receipt');
            return {
                fileName: raw.fileName ?? raw.FileName,
                contentType: raw.contentType ?? raw.ContentType,
                base64: raw.base64 ?? raw.Base64,
            };
        });
    }

    approvePayment(id: number, note?: string): Promise<TenantSubscriptionDto> {
        return this.post('/ApprovePayment', { id, note: note || null }, 'Failed to approve the payment')
            .then((x) => this.mapSubscription(x));
    }

    rejectPayment(id: number, note: string): Promise<void> {
        return this.post('/RejectPayment', { id, note }, 'Failed to reject the payment').then(() => undefined);
    }

    getAccessStatus(force = false): Promise<{
        expired: boolean;
        planName?: string;
        amount: number;
        paidUntil?: string | null;
        daysLeft: number;
        hasPendingPayment: boolean;
    }> {
        if (!force && this.accessCache && Date.now() - this.accessCache.at < 15000) {
            return Promise.resolve(this.accessCache.value);
        }
        return this.readAccessStatus().then((value) => {
            this.accessCache = { at: Date.now(), value };
            return value;
        });
    }

    private readAccessStatus(): Promise<{
        expired: boolean;
        planName?: string;
        amount: number;
        paidUntil?: string | null;
        daysLeft: number;
        hasPendingPayment: boolean;
    }> {
        return firstValueFrom(this.http.get<any>(`${this.apiUrl}/GetAccessStatus`)).then((res) => {
            const raw = this.unwrap(res, 'Failed to check the plan');
            return {
                expired: !!(raw.expired ?? raw.Expired),
                planName: raw.planName ?? raw.PlanName,
                amount: raw.amount ?? raw.Amount ?? 0,
                paidUntil: raw.paidUntil ?? raw.PaidUntil,
                daysLeft: raw.daysLeft ?? raw.DaysLeft ?? 0,
                hasPendingPayment: !!(raw.hasPendingPayment ?? raw.HasPendingPayment),
            };
        });
    }

    getMine(): Promise<MySubscriptionDto> {
        return firstValueFrom(this.http.get<any>(`${this.apiUrl}/GetMine`)).then((res) => {
            const raw = this.unwrap(res, 'Failed to load billing');
            return {
                subscription: this.mapSubscription(raw.subscription ?? raw.Subscription),
                paymentMethods: (raw.paymentMethods ?? raw.PaymentMethods ?? []).map((x: any) => this.mapMethod(x)),
                payments: (raw.payments ?? raw.Payments ?? []).map((x: any) => this.mapPayment(x)),
                availablePlans: (raw.availablePlans ?? raw.AvailablePlans ?? []).map((x: any) => this.mapPlan(x)),
            };
        });
    }

    submitPayment(input: {
        packageId: number;
        cycle: number;
        paymentMethodId: number;
        receiptBase64: string;
        receiptContentType: string;
        receiptFileName: string;
    }): Promise<SubscriptionPaymentDto> {
        return this.post('/SubmitPayment', input, 'Failed to submit the receipt')
            .then((x) => this.mapPayment(x));
    }

    private mapPlan(raw: any): AssignablePlanDto {
        return {
            id: raw.id ?? raw.Id,
            name: raw.name ?? raw.Name,
            monthlyAmount: Number(raw.monthlyAmount ?? raw.MonthlyAmount ?? 0),
            yearlyAmount: Number(raw.yearlyAmount ?? raw.YearlyAmount ?? 0),
        };
    }

    private async getList<T>(path: string, fallback: string, map: (raw: any) => T): Promise<T[]> {
        const res: any = await firstValueFrom(this.http.get<any>(`${this.apiUrl}${path}`));
        const result = this.unwrap(res, fallback);
        const items = Array.isArray(result) ? result : result?.items || [];
        return items.map(map);
    }

    private async post(path: string, body: any, fallback: string): Promise<any> {
        const res: any = await firstValueFrom(this.http.post<any>(`${this.apiUrl}${path}`, body));
        if (res == null) {
            return null;
        }
        return this.unwrap(res, fallback);
    }

    private mapMethod(raw: any): PaymentMethodDto {
        return {
            id: raw.id ?? raw.Id,
            kind: raw.kind ?? raw.Kind,
            kindName: raw.kindName ?? raw.KindName,
            accountTitle: raw.accountTitle ?? raw.AccountTitle,
            accountNumber: raw.accountNumber ?? raw.AccountNumber,
            bankName: raw.bankName ?? raw.BankName,
            instructions: raw.instructions ?? raw.Instructions,
            isActive: raw.isActive ?? raw.IsActive,
            hasQr: raw.hasQr ?? raw.HasQr,
            qrBase64: raw.qrBase64 ?? raw.QrBase64,
            qrContentType: raw.qrContentType ?? raw.QrContentType,
        };
    }

    private mapSubscription(raw: any): TenantSubscriptionDto {
        return {
            id: raw?.id ?? raw?.Id,
            tenantId: raw?.tenantId ?? raw?.TenantId,
            tenancyName: raw?.tenancyName ?? raw?.TenancyName,
            tenantName: raw?.tenantName ?? raw?.TenantName,
            packageId: raw?.packageId ?? raw?.PackageId,
            planName: raw?.planName ?? raw?.PlanName,
            cycle: raw?.cycle ?? raw?.Cycle ?? 0,
            cycleName: raw?.cycleName ?? raw?.CycleName,
            amount: raw?.amount ?? raw?.Amount ?? 0,
            paidUntil: raw?.paidUntil ?? raw?.PaidUntil,
            daysLeft: raw?.daysLeft ?? raw?.DaysLeft ?? 0,
            status: raw?.status ?? raw?.Status ?? 'Unassigned',
            canPay: raw?.canPay ?? raw?.CanPay ?? false,
            hasPendingPayment: raw?.hasPendingPayment ?? raw?.HasPendingPayment ?? false,
        };
    }

    private mapPayment(raw: any): SubscriptionPaymentDto {
        return {
            id: raw.id ?? raw.Id,
            tenantId: raw.tenantId ?? raw.TenantId,
            tenancyName: raw.tenancyName ?? raw.TenancyName,
            tenantName: raw.tenantName ?? raw.TenantName,
            planName: raw.planName ?? raw.PlanName,
            cycle: raw.cycle ?? raw.Cycle,
            cycleName: raw.cycleName ?? raw.CycleName,
            amount: raw.amount ?? raw.Amount ?? 0,
            method: raw.method ?? raw.Method,
            methodName: raw.methodName ?? raw.MethodName,
            status: raw.status ?? raw.Status,
            statusName: raw.statusName ?? raw.StatusName,
            reviewNote: raw.reviewNote ?? raw.ReviewNote,
            creationTime: raw.creationTime ?? raw.CreationTime,
            reviewedTime: raw.reviewedTime ?? raw.ReviewedTime,
            hasReceipt: raw.hasReceipt ?? raw.HasReceipt,
        };
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }
}

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    IsTenantAvailableInput,
    IsTenantAvailableOutput,
    SignUpTenantInput,
    SignUpTenantOutput,
} from '../api/account';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class AccountService {
    private readonly baseUrl = `${environment.apiUrl}/api/services/app/Account`;

    constructor(private http: HttpClient) {}

    async isTenantAvailable(
        input: IsTenantAvailableInput
    ): Promise<IsTenantAvailableOutput> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.baseUrl}/IsTenantAvailable`, {
                tenancyName: input.tenancyName,
            })
        );
        const result = this.unwrap(res, 'Failed to check tenancy name');
        return {
            state: result.state ?? result.State,
            tenantId: result.tenantId ?? result.TenantId,
        };
    }

    async signUpTenant(input: SignUpTenantInput): Promise<SignUpTenantOutput> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(`${this.baseUrl}/SignUpTenant`, {
                    tenancyName: input.tenancyName?.trim(),
                    name: input.name?.trim(),
                    adminName: input.adminName?.trim(),
                    adminSurname: input.adminSurname?.trim(),
                    adminEmailAddress: input.adminEmailAddress?.trim(),
                    adminUserName: input.adminUserName?.trim(),
                    adminPassword: input.adminPassword,
                })
            );
            const result = this.unwrap(res, 'Signup failed');
            return {
                tenantId: result.tenantId ?? result.TenantId,
                tenancyName: result.tenancyName ?? result.TenancyName,
                name: result.name ?? result.Name,
                adminUserName: result.adminUserName ?? result.AdminUserName,
                canLogin: result.canLogin ?? result.CanLogin ?? true,
            };
        } catch (error: any) {
            throw new Error(
                this.extractErrorMessage(error, 'Signup failed. Please try again.')
            );
        }
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
        return res.result ?? res;
    }

    private extractErrorMessage(error: any, fallback: string): string {
        const abpError = error?.error;
        return (
            abpError?.error?.message ||
            abpError?.message ||
            abpError?.details ||
            error?.message ||
            fallback
        );
    }
}

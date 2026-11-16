import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface OnlineUserDto {
    userId: number;
    userName?: string;
    fullName?: string;
    roleNames?: string;
    branchId?: number | null;
    branchName?: string;
    tenantId?: number | null;
    tenancyName?: string;
    loginTime?: string;
    lastSeen?: string;
}

@Injectable({ providedIn: 'root' })
export class UserPresenceService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/UserPresence`;
    private timer: ReturnType<typeof setInterval> | null = null;

    constructor(private http: HttpClient) {}

    startHeartbeat(isAuthenticated: () => boolean): void {
        this.stopHeartbeat();
        if (!isAuthenticated()) {
            return;
        }
        void this.ping();
        this.timer = setInterval(() => {
            if (isAuthenticated()) {
                void this.ping();
            }
        }, 45_000);
    }

    stopHeartbeat(): void {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
    }

    /** Fire while the access token is still stored. */
    signOut(): void {
        this.stopHeartbeat();
        this.http.post(`${this.apiUrl}/SignOut`, {}).subscribe({
            error: () => undefined,
        });
    }

    async ping(): Promise<void> {
        try {
            await firstValueFrom(this.http.post(`${this.apiUrl}/Ping`, {}));
        } catch {
            // Presence is best-effort. A failed ping just drops this user from the list.
        }
    }

    async getOnline(): Promise<OnlineUserDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetOnline`)
        );
        const result = this.unwrap(res, 'Failed to load signed-in users');
        const items = Array.isArray(result) ? result : result?.items || [];
        return (items || []).map((x: any) => this.mapUser(x));
    }

    private mapUser(raw: any): OnlineUserDto {
        return {
            userId: raw.userId ?? raw.UserId,
            userName: raw.userName ?? raw.UserName,
            fullName: raw.fullName ?? raw.FullName,
            roleNames: raw.roleNames ?? raw.RoleNames,
            branchId: raw.branchId ?? raw.BranchId,
            branchName: raw.branchName ?? raw.BranchName,
            tenantId: raw.tenantId ?? raw.TenantId,
            tenancyName: raw.tenancyName ?? raw.TenancyName,
            loginTime: raw.loginTime ?? raw.LoginTime,
            lastSeen: raw.lastSeen ?? raw.LastSeen,
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

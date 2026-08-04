import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthenticateModel, AuthenticateResultModel } from '../api/auth';
import { environment } from '../../../environments/environment';
import { PermissionService } from './permission.service';

const TOKEN_KEY = 'accessToken';
const ENCRYPTED_TOKEN_KEY = 'encryptedAccessToken';
const USER_ID_KEY = 'userId';
const EXPIRE_KEY = 'expireInSeconds';
const USER_INFO_KEY = 'userInfo';

export interface ExternalLoginProviderInfo {
    name: string;
    clientId: string;
}

export interface ExternalAuthenticateModel {
    authProvider: string;
    providerKey?: string;
    providerAccessCode: string;
}

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private readonly authenticateUrl = `${environment.apiUrl}/api/TokenAuth/Authenticate`;
    private readonly externalAuthenticateUrl = `${environment.apiUrl}/api/TokenAuth/ExternalAuthenticate`;
    private readonly externalProvidersUrl = `${environment.apiUrl}/api/TokenAuth/GetExternalAuthenticationProviders`;
    private readonly sendEmailLoginCodeUrl = `${environment.apiUrl}/api/TokenAuth/SendEmailLoginCode`;
    private readonly authenticateWithEmailCodeUrl = `${environment.apiUrl}/api/TokenAuth/AuthenticateWithEmailCode`;

    constructor(
        private http: HttpClient,
        private permissionService: PermissionService
    ) {}

    async authenticate(model: AuthenticateModel): Promise<AuthenticateResultModel> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(this.authenticateUrl, {
                    userNameOrEmailAddress: model.userNameOrEmailAddress,
                    password: model.password,
                    rememberClient: !!model.rememberClient,
                })
            );

            return this.persistAuthResult(res, 'Authentication failed');
        } catch (error: any) {
            throw new Error(this.extractErrorMessage(error, 'Login failed. Please check your credentials.'));
        }
    }

    async sendEmailLoginCode(emailAddress: string): Promise<{ expirationMinutes: number; resendCooldownSeconds: number }> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(this.sendEmailLoginCodeUrl, {
                    emailAddress: (emailAddress || '').trim(),
                })
            );

            if (!res) {
                throw new Error('No response from server');
            }
            if (res.success === false || res.error) {
                throw new Error(
                    res.error?.message || res.error?.details || 'Failed to send sign-in code'
                );
            }

            const result = res.result ?? res;
            return {
                expirationMinutes:
                    result.expirationMinutes ?? result.ExpirationMinutes ?? 5,
                resendCooldownSeconds:
                    result.resendCooldownSeconds ?? result.ResendCooldownSeconds ?? 60,
            };
        } catch (error: any) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to send sign-in code. Please try again.')
            );
        }
    }

    async authenticateWithEmailCode(
        emailAddress: string,
        code: string
    ): Promise<AuthenticateResultModel> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(this.authenticateWithEmailCodeUrl, {
                    emailAddress: (emailAddress || '').trim(),
                    code: (code || '').trim(),
                })
            );

            return this.persistAuthResult(res, 'Email code authentication failed');
        } catch (error: any) {
            throw new Error(
                this.extractErrorMessage(error, 'Invalid or expired sign-in code.')
            );
        }
    }

    async getExternalAuthenticationProviders(): Promise<ExternalLoginProviderInfo[]> {
        try {
            const res: any = await firstValueFrom(this.http.get<any>(this.externalProvidersUrl));
            const result = res?.result ?? res;
            const list = Array.isArray(result) ? result : [];
            return list.map((p: any) => ({
                name: p.name ?? p.Name,
                clientId: p.clientId ?? p.ClientId,
            }));
        } catch {
            return [];
        }
    }

    async externalAuthenticate(model: ExternalAuthenticateModel): Promise<AuthenticateResultModel> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(this.externalAuthenticateUrl, {
                    authProvider: model.authProvider,
                    providerKey: model.providerKey || '',
                    providerAccessCode: model.providerAccessCode,
                })
            );

            if (!res) {
                throw new Error('No response from server');
            }

            if (res.success === false || res.error) {
                throw new Error(
                    res.error?.message ||
                        res.error?.details ||
                        'External authentication failed'
                );
            }

            const result = res.result ?? res;
            if (result.waitingForActivation || result.WaitingForActivation) {
                throw new Error('Your account is waiting for activation.');
            }

            return this.persistAuthResult(res, 'External authentication failed');
        } catch (error: any) {
            throw new Error(
                this.extractErrorMessage(error, 'Google sign-in failed. Please try again.')
            );
        }
    }

    getAccessToken(): string | null {
        return localStorage.getItem(TOKEN_KEY);
    }

    isAuthenticated(): boolean {
        return !!this.getAccessToken();
    }

    logout(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(ENCRYPTED_TOKEN_KEY);
        localStorage.removeItem(USER_ID_KEY);
        localStorage.removeItem(EXPIRE_KEY);
        localStorage.removeItem(USER_INFO_KEY);
        localStorage.removeItem('SmartPos.BranchId');
        this.permissionService.clear();
    }

    setUserInfo(userInfo: any): void {
        localStorage.setItem(USER_INFO_KEY, JSON.stringify(userInfo));
    }

    getUserInfo(): any {
        const userInfo = localStorage.getItem(USER_INFO_KEY);
        return userInfo ? JSON.parse(userInfo) : null;
    }

    private persistAuthResult(res: any, fallbackMessage: string): AuthenticateResultModel {
        if (!res) {
            throw new Error('No response from server');
        }

        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }

        const result = res.result ?? res;
        const accessToken = result.accessToken ?? result.AccessToken;
        const encryptedAccessToken =
            result.encryptedAccessToken ?? result.EncryptedAccessToken ?? '';
        const userId = result.userId ?? result.UserId ?? 0;
        const expireInSeconds =
            result.expireInSeconds ?? result.ExpireInSeconds ?? 0;

        if (!accessToken) {
            throw new Error(
                'Invalid response: access token not found. Please check your credentials.'
            );
        }

        localStorage.removeItem(USER_INFO_KEY);
        this.permissionService.clear();
        localStorage.setItem(TOKEN_KEY, accessToken);
        if (encryptedAccessToken) {
            localStorage.setItem(ENCRYPTED_TOKEN_KEY, encryptedAccessToken);
        }
        if (userId != null) {
            localStorage.setItem(USER_ID_KEY, String(userId));
        }
        if (expireInSeconds != null) {
            localStorage.setItem(EXPIRE_KEY, String(expireInSeconds));
        }

        return {
            accessToken,
            encryptedAccessToken,
            expireInSeconds,
            userId,
        };
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

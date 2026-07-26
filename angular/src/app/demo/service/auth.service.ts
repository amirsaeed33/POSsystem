import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthenticateModel, AuthenticateResultModel } from '../api/auth';
import { environment } from '../../../environments/environment';

const TOKEN_KEY = 'accessToken';
const ENCRYPTED_TOKEN_KEY = 'encryptedAccessToken';
const USER_ID_KEY = 'userId';
const EXPIRE_KEY = 'expireInSeconds';
const USER_INFO_KEY = 'userInfo';

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private readonly authenticateUrl = `${environment.apiUrl}/api/TokenAuth/Authenticate`;

    constructor(private http: HttpClient) {}

    async authenticate(model: AuthenticateModel): Promise<AuthenticateResultModel> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(this.authenticateUrl, {
                    userNameOrEmailAddress: model.userNameOrEmailAddress,
                    password: model.password,
                    rememberClient: !!model.rememberClient,
                })
            );

            if (!res) {
                throw new Error('No response from server');
            }

            // ASP.NET Zero / ABP wraps payloads as { success, result, error }
            if (res.success === false || res.error) {
                throw new Error(
                    res.error?.message ||
                        res.error?.details ||
                        'Authentication failed'
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
        } catch (error: any) {
            const abpError = error?.error;
            const errorMessage =
                abpError?.error?.message ||
                abpError?.message ||
                abpError?.details ||
                error?.message ||
                'Login failed. Please check your credentials.';

            throw new Error(errorMessage);
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
    }

    setUserInfo(userInfo: any): void {
        localStorage.setItem(USER_INFO_KEY, JSON.stringify(userInfo));
    }

    getUserInfo(): any {
        const userInfo = localStorage.getItem(USER_INFO_KEY);
        return userInfo ? JSON.parse(userInfo) : null;
    }
}

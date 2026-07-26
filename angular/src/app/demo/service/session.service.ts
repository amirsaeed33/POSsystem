import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { GetCurrentLoginInformationsOutput, UserLoginInfoDto } from '../api/session';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root',
})
export class SessionService {
    private apiUrl = `${environment.apiUrl}/api/services/app`;
    private userApiUrl = `${environment.apiUrl}/api/services/app/User`;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    getCurrentLoginInformations(): Promise<GetCurrentLoginInformationsOutput> {
        const headers = this.getAuthHeaders();
        
        return this.http.get<any>(`${this.apiUrl}/Session/GetCurrentLoginInformations`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // ABP Framework wraps responses in a result property
                const result = res.result || res;
                
                const sessionInfo: GetCurrentLoginInformationsOutput = {
                    application: result.application || result.Application,
                    user: result.user || result.User,
                    tenant: result.tenant || result.Tenant
                };

                // If we have a user, also fetch their roles and profile picture
                if (sessionInfo.user?.id) {
                    return this.getCurrentUserWithRoles(sessionInfo.user.id)
                        .then((userWithRoles) => {
                            if (userWithRoles) {
                                if (userWithRoles.roleNames && userWithRoles.roleNames.length > 0) {
                                    sessionInfo.user.roleNames = userWithRoles.roleNames;
                                }
                                if (userWithRoles.profilePictureUrl) {
                                    sessionInfo.user.profilePictureUrl = userWithRoles.profilePictureUrl;
                                }
                            }
                            return sessionInfo;
                        })
                        .catch((error) => {
                            // If role fetch fails, return session info without roles
                            return sessionInfo;
                        });
                }

                return sessionInfo;
            }) as Promise<GetCurrentLoginInformationsOutput>;
    }

    private getCurrentUserWithRoles(userId: number): Promise<any> {
        const headers = this.getAuthHeaders();
        
        return this.http.get<any>(`${this.userApiUrl}/Get?Id=${userId}`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    return null;
                }

                const result = res.result || res;
                const roleNames = result.roleNames || result.RoleNames || [];
                const profilePictureUrl = result.profilePictureUrl || result.ProfilePictureUrl || null;
                
                return {
                    roleNames: Array.isArray(roleNames) ? roleNames : [],
                    profilePictureUrl: profilePictureUrl
                };
            })
            .catch((error) => {
                // If getting user fails, try to get roles from session or return empty
                return { roleNames: [], profilePictureUrl: null };
            });
    }

    private getAuthHeaders(): HttpHeaders {
        const token = this.authService.getAccessToken();
        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }
        return headers;
    }
}


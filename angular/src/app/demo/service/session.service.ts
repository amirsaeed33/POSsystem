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
                
                const rawUser = result.user || result.User;
                const sessionInfo: GetCurrentLoginInformationsOutput = {
                    application: result.application || result.Application,
                    user: this.normalizeSessionUser(rawUser),
                    tenant: result.tenant || result.Tenant
                };

                // If we have a user, also fetch their roles and profile picture
                const currentUser = sessionInfo.user;
                if (currentUser?.id) {
                    return this.getCurrentUserWithRoles(currentUser.id)
                        .then((userWithRoles) => {
                            if (userWithRoles) {
                                if (userWithRoles.roleNames && userWithRoles.roleNames.length > 0) {
                                    currentUser.roleNames = userWithRoles.roleNames;
                                }
                                if (userWithRoles.profilePictureUrl) {
                                    currentUser.profilePictureUrl = userWithRoles.profilePictureUrl;
                                }
                            }
                            return sessionInfo;
                        })
                        .catch(() => {
                            // If role fetch fails, return session info without roles
                            return sessionInfo;
                        });
                }

                return sessionInfo;
            }) as Promise<GetCurrentLoginInformationsOutput>;
    }

    /** Backend uses userImageUrl; UI/navbar uses profilePictureUrl. */
    private resolveProfilePictureUrl(source: any): string | null {
        if (!source) {
            return null;
        }
        return (
            source.profilePictureUrl ||
            source.ProfilePictureUrl ||
            source.userImageUrl ||
            source.UserImageUrl ||
            null
        );
    }

    private normalizeSessionUser(rawUser: any): UserLoginInfoDto | null {
        if (!rawUser) {
            return null;
        }

        const branchId = rawUser.branchId ?? rawUser.BranchId;
        return {
            id: rawUser.id ?? rawUser.Id,
            name: rawUser.name ?? rawUser.Name,
            surname: rawUser.surname ?? rawUser.Surname,
            userName: rawUser.userName ?? rawUser.UserName,
            emailAddress: rawUser.emailAddress ?? rawUser.EmailAddress,
            roleNames: rawUser.roleNames || rawUser.RoleNames || [],
            profilePictureUrl: this.resolveProfilePictureUrl(rawUser) || undefined,
            branchId: branchId != null ? Number(branchId) : null,
            branchName: rawUser.branchName ?? rawUser.BranchName,
        };
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
                
                return {
                    roleNames: Array.isArray(roleNames) ? roleNames : [],
                    profilePictureUrl: this.resolveProfilePictureUrl(result)
                };
            })
            .catch(() => {
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


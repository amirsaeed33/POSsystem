import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { UserDto, CreateUserDto, PagedUserResultRequestDto, PagedResultDto, RoleDto, UpdateUserPermissionsInput, ChangePasswordDto, ResetPasswordDto } from '../api/user-management';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { firstValueFrom } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class UserService {
    private apiUrl = `${environment.apiUrl}/api/services/app/User`;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    getAll(input?: PagedUserResultRequestDto): Promise<PagedResultDto<UserDto>> {
        const headers = this.getAuthHeaders();
        const params: any = {};
        
        if (input) {
            if (input.keyword) params.keyword = input.keyword;
            if (input.isActive !== undefined) params.isActive = input.isActive;
            if (input.sorting) params.sorting = input.sorting;
            if (input.skipCount !== undefined) params.skipCount = input.skipCount;
            if (input.maxResultCount !== undefined) params.maxResultCount = input.maxResultCount;
        }

        // ABP Framework exposes GetAll method explicitly
        return this.http.get<any>(`${this.apiUrl}/GetAll`, { headers, params })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to load users';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                
                // Handle both direct result and wrapped result
                const items = result.items || result.Items || result || [];
                const totalCount = result.totalCount || result.TotalCount || items.length;
                
                return {
                    items: Array.isArray(items) ? items.map((item: any) => this.mapUserDto(item)) : [],
                    totalCount: totalCount
                } as PagedResultDto<UserDto>;
            })
            .catch((error: any) => {
                // Handle HTTP errors
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to load users';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<PagedResultDto<UserDto>>;
    }

    get(id: number): Promise<UserDto> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/Get?Id=${id}`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to get user';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapUserDto(result);
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to get user';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<UserDto>;
    }

    create(input: CreateUserDto | any): Promise<UserDto> {
        const headers = this.getAuthHeaders();
        const body = this.toApiUserPayload(input, true);
        return this.http.post<any>(`${this.apiUrl}/Create`, body, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to create user';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapUserDto(result);
            })
            .catch((error: any) => {
                // Handle HTTP errors
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to create user';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<UserDto>;
    }

    update(input: UserDto | any): Promise<UserDto> {
        const headers = this.getAuthHeaders();
        const body = this.toApiUserPayload(input, false);
        return this.http.put<any>(`${this.apiUrl}/Update`, body, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to update user';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapUserDto(result);
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to update user';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<UserDto>;
    }

    delete(id: number): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.delete<any>(`${this.apiUrl}/Delete`, { headers, params: { Id: String(id) } })
            .toPromise()
            .then((res: any) => {
                if (res && (res.error || res.success === false)) {
                    throw new Error(res.error?.message || res.error?.details || 'Failed to delete user');
                }
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    throw new Error(
                        abpError.error?.message ||
                        abpError.message ||
                        abpError.details ||
                        error.message ||
                        'Failed to delete user'
                    );
                }
                throw error;
            }) as Promise<void>;
    }

    activate(id: number): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.post<any>(`${this.apiUrl}/Activate`, { id }, { headers })
            .toPromise()
            .then(() => {});
    }

    deactivate(id: number): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.post<any>(`${this.apiUrl}/DeActivate`, { id }, { headers })
            .toPromise()
            .then(() => {});
    }

    getRoles(): Promise<RoleDto[]> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/GetRoles`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                const result = res.result || res;
                const items = result.items || result.Items || [];
                return items.map((item: any) => ({
                    id: item.id || item.Id,
                    name: item.name || item.Name,
                    displayName: item.displayName || item.DisplayName,
                    normalizedName: item.normalizedName || item.NormalizedName,
                    description: item.description || item.Description
                })) as RoleDto[];
            }) as Promise<RoleDto[]>;
    }

    getUserPermissions(userId: number): Promise<string[]> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/GetUserPermissions?Id=${userId}`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to get user permissions';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                const items = result.items || result.Items || [];
                return items.map((item: any) => item || item) as string[];
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to get user permissions';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<string[]>;
    }

    updateUserPermissions(input: UpdateUserPermissionsInput): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.put<any>(`${this.apiUrl}/UpdateUserPermissions`, input, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to update user permissions';
                    throw new Error(errorMessage);
                }
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to update user permissions';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<void>;
    }

    async changePassword(input: ChangePasswordDto): Promise<boolean> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(`${this.apiUrl}/ChangePassword`, {
                    currentPassword: input.currentPassword,
                    newPassword: input.newPassword,
                })
            );
            return this.unwrapBoolean(res, 'Failed to change password');
        } catch (error: any) {
            throw new Error(this.extractErrorMessage(error, 'Failed to change password'));
        }
    }

    async resetPassword(input: ResetPasswordDto): Promise<boolean> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(`${this.apiUrl}/ResetPassword`, {
                    adminPassword: input.adminPassword,
                    userId: input.userId,
                    newPassword: input.newPassword,
                })
            );
            return this.unwrapBoolean(res, 'Failed to reset password');
        } catch (error: any) {
            throw new Error(this.extractErrorMessage(error, 'Failed to reset password'));
        }
    }

    private unwrapBoolean(res: any, fallback: string): boolean {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallback
            );
        }
        const result = res.result ?? res;
        if (result === true || result === 'true') {
            return true;
        }
        if (res.success === true) {
            return true;
        }
        return !!result;
    }

    private extractErrorMessage(error: any, fallback: string): string {
        const abpError = error?.error;
        // Empty-body 400 is often antiforgery rejection (no JSON details).
        if (error?.status === 400 && (abpError == null || abpError === '')) {
            return 'Request rejected (400). Refresh the page and try again.';
        }
        const validationErrors = abpError?.error?.validationErrors;
        if (Array.isArray(validationErrors) && validationErrors.length > 0) {
            const messages = validationErrors
                .map((v: any) => v?.message)
                .filter((m: string) => !!m);
            if (messages.length > 0) {
                return messages.join(' ');
            }
        }
        return (
            abpError?.error?.details ||
            abpError?.error?.message ||
            abpError?.message ||
            abpError?.details ||
            (typeof abpError === 'string' && abpError) ||
            fallback
        );
    }

    private mapUserDto(item: any): UserDto {
        return {
            id: item.id || item.Id,
            userName: item.userName || item.UserName,
            name: item.name || item.Name,
            surname: item.surname || item.Surname,
            emailAddress: item.emailAddress || item.EmailAddress,
            isActive: item.isActive !== undefined ? item.isActive : item.IsActive,
            fullName: item.fullName || item.FullName || '',
            lastLoginTime: item.lastLoginTime || item.LastLoginTime,
            creationTime: item.creationTime || item.CreationTime,
            roleNames: item.roleNames || item.RoleNames || [],
            // Backend field is userImageUrl; older clients may send profilePictureUrl
            profilePictureUrl:
                item.profilePictureUrl ||
                item.ProfilePictureUrl ||
                item.userImageUrl ||
                item.UserImageUrl ||
                null,
            branchId:
                item.branchId != null || item.BranchId != null
                    ? Number(item.branchId ?? item.BranchId)
                    : null,
            branchName: item.branchName || item.BranchName,
        };
    }

    /** Map UI form fields to backend User Create/Update DTO (ImageBase64, not profilePictureUrl). */
    private toApiUserPayload(input: any, isCreate: boolean): any {
        const body: any = {
            userName: input.userName,
            name: input.name,
            surname: input.surname,
            emailAddress: input.emailAddress,
            isActive: input.isActive !== false,
            roleNames: Array.isArray(input.roleNames) ? input.roleNames : [],
            branchId:
                input.branchId != null && input.branchId !== ''
                    ? Number(input.branchId)
                    : null,
        };

        if (input.id != null) {
            body.id = input.id;
        }

        if (isCreate) {
            body.password = input.password;
        } else if (input.password && String(input.password).trim() !== '') {
            // Backend UserDto has no password; password changes go through ResetPassword.
            // Ignore accidental password field on update.
        }

        if (
            input.imageBase64 &&
            typeof input.imageBase64 === 'string' &&
            input.imageBase64.startsWith('data:image')
        ) {
            body.imageBase64 = input.imageBase64;
        }

        return body;
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


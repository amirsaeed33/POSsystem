import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RoleDto, CreateRoleDto, PagedRoleResultRequestDto, PagedResultDto, RoleListDto, PermissionDto, GetRoleForEditOutput } from '../api/role-management';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root',
})
export class RoleService {
    private apiUrl = `${environment.apiUrl}/api/services/app/Role`;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    getAll(input?: PagedRoleResultRequestDto): Promise<PagedResultDto<RoleDto>> {
        const headers = this.getAuthHeaders();
        const params: any = {};
        
        if (input) {
            if (input.keyword) params.keyword = input.keyword;
            if (input.sorting) params.sorting = input.sorting;
            if (input.skipCount !== undefined) params.skipCount = input.skipCount;
            if (input.maxResultCount !== undefined) params.maxResultCount = input.maxResultCount;
        }

        return this.http.get<any>(`${this.apiUrl}/GetAll`, { headers, params })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to load roles';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                const items = result.items || result.Items || result || [];
                const totalCount = result.totalCount || result.TotalCount || items.length;
                
                return {
                    items: Array.isArray(items) ? items.map((item: any) => this.mapRoleDto(item)) : [],
                    totalCount: totalCount
                } as PagedResultDto<RoleDto>;
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to load roles';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<PagedResultDto<RoleDto>>;
    }

    get(id: number): Promise<RoleDto> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/Get?Id=${id}`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to get role';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapRoleDto(result);
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to get role';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<RoleDto>;
    }

    getRoleForEdit(id: number): Promise<GetRoleForEditOutput> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/GetRoleForEdit?Id=${id}`, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to get role for edit';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return {
                    role: this.mapRoleDto(result.role || result.Role),
                    permissions: (result.permissions || result.Permissions || []).map((p: any) => ({
                        name: p.name || p.Name,
                        displayName: p.displayName || p.DisplayName,
                        description: p.description || p.Description,
                        parentName: p.parentName || p.ParentName
                    })),
                    grantedPermissionNames: result.grantedPermissionNames || result.GrantedPermissionNames || []
                } as GetRoleForEditOutput;
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to get role for edit';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<GetRoleForEditOutput>;
    }

    create(input: CreateRoleDto): Promise<RoleDto> {
        const headers = this.getAuthHeaders();
        return this.http.post<any>(`${this.apiUrl}/Create`, input, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to create role';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapRoleDto(result);
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to create role';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<RoleDto>;
    }

    update(input: RoleDto): Promise<RoleDto> {
        const headers = this.getAuthHeaders();
        return this.http.put<any>(`${this.apiUrl}/Update`, input, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to update role';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                return this.mapRoleDto(result);
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to update role';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<RoleDto>;
    }

    delete(id: number): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.delete<any>(`${this.apiUrl}/Delete`, { headers, params: { Id: String(id) } })
            .toPromise()
            .then((res: any) => {
                if (res && (res.error || res.success === false)) {
                    throw new Error(res.error?.message || res.error?.details || 'Failed to delete role');
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
                        'Failed to delete role'
                    );
                }
                throw error;
            }) as Promise<void>;
    }

    getAllPermissions(): Promise<PermissionDto[]> {
        const headers = this.getAuthHeaders();
        return this.http.get<any>(`${this.apiUrl}/GetAllPermissions`, { headers })
            .toPromise()
            .then((res: any) => {
                // Handle 204 No Content or null/undefined response
                // 204 responses typically return null/undefined body
                if (!res || res === null || res === undefined) {
                    return [] as PermissionDto[];
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to load permissions';
                    throw new Error(errorMessage);
                }

                const result = res.result || res;
                
                // If result is null/undefined, return empty array
                if (!result) {
                    return [] as PermissionDto[];
                }

                // Handle different response structures
                let items: any[] = [];
                if (result.items) {
                    items = result.items;
                } else if (result.Items) {
                    items = result.Items;
                } else if (Array.isArray(result)) {
                    items = result;
                } else if (result && typeof result === 'object') {
                    // Try to find array property
                    const keys = Object.keys(result);
                    const arrayKey = keys.find(k => Array.isArray(result[k]));
                    if (arrayKey) {
                        items = result[arrayKey];
                    }
                }

                if (!Array.isArray(items)) {
                    items = [];
                }

                return items.map((item: any) => {
                    const name = item.name || item.Name || '';
                    const displayName = item.displayName || item.DisplayName || item.name || item.Name || '';
                    
                    // Extract parent name from permission name structure (e.g., "Pages.Roles" -> "Pages")
                    // If parentName is not provided, derive it from the name
                    let parentName = item.parentName || item.ParentName || '';
                    if (!parentName && name) {
                        const parts = name.split('.');
                        if (parts.length > 1) {
                            // Take all parts except the last one as parent
                            parentName = parts.slice(0, -1).join('.');
                        } else {
                            parentName = 'Other';
                        }
                    }
                    
                    return {
                        name: name,
                        displayName: displayName,
                        description: item.description || item.Description || '',
                        parentName: parentName || 'Other'
                    };
                }) as PermissionDto[];
            })
            .catch((error: any) => {
                // Handle 204 No Content as a valid response (empty permissions)
                // Angular HttpClient may throw an error for 204, or status might be in error object
                if (error?.status === 204 || error?.status === 0 || 
                    (error?.error && error.error.status === 204)) {
                    return [] as PermissionDto[];
                }

                if (error?.error) {
                    const abpError = error.error;
                    // Check if it's a 204 status in the error
                    if (abpError.status === 204) {
                        return [] as PermissionDto[];
                    }
                    const errorMessage = abpError.error?.message || 
                                       abpError.message || 
                                       abpError.details ||
                                       error.message || 
                                       'Failed to load permissions';
                    throw new Error(errorMessage);
                }
                throw error;
            }) as Promise<PermissionDto[]>;
    }

    private mapRoleDto(item: any): RoleDto {
        return {
            id: item.id || item.Id,
            name: item.name || item.Name,
            displayName: item.displayName || item.DisplayName,
            normalizedName: item.normalizedName || item.NormalizedName,
            description: item.description || item.Description,
            grantedPermissions: item.grantedPermissions || item.GrantedPermissions || []
        };
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


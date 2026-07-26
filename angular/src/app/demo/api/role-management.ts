export interface RoleDto {
    id: number;
    name: string;
    displayName: string;
    normalizedName?: string;
    description?: string;
    grantedPermissions?: string[];
}

export interface CreateRoleDto {
    name: string;
    displayName: string;
    normalizedName?: string;
    description?: string;
    grantedPermissions?: string[];
}

export interface RoleListDto {
    id: number;
    name: string;
    displayName: string;
    isStatic: boolean;
    isDefault: boolean;
    creationTime: string;
}

export interface PagedRoleResultRequestDto {
    keyword?: string;
    sorting?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

export interface PermissionDto {
    name: string;
    displayName: string;
    description?: string;
    parentName?: string;
}

export interface GetRoleForEditOutput {
    role: RoleDto;
    permissions: PermissionDto[];
    grantedPermissionNames: string[];
}


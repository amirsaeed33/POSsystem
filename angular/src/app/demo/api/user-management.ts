export interface UserDto {
    id: number;
    userName: string;
    name: string;
    surname: string;
    emailAddress: string;
    isActive: boolean;
    fullName: string;
    lastLoginTime?: string;
    creationTime: string;
    roleNames?: string[];
    profilePictureUrl?: string;
}

export interface CreateUserDto {
    userName: string;
    name: string;
    surname: string;
    emailAddress: string;
    isActive: boolean;
    roleNames?: string[];
    password: string;
    profilePictureUrl?: string;
}

export interface PagedUserResultRequestDto {
    keyword?: string;
    isActive?: boolean;
    sorting?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

export interface RoleDto {
    id: number;
    name: string;
    displayName: string;
    normalizedName: string;
    description?: string;
}

export interface UpdateUserPermissionsInput {
    id: number;
    grantedPermissionNames: string[];
}


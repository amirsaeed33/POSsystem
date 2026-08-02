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
    branchIds?: number[];
    profilePictureUrl?: string;
}

export interface CreateUserDto {
    userName: string;
    name: string;
    surname: string;
    emailAddress: string;
    isActive: boolean;
    roleNames?: string[];
    branchIds?: number[];
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

export interface ChangePasswordDto {
    currentPassword: string;
    newPassword: string;
}

export interface ResetPasswordDto {
    adminPassword: string;
    userId: number;
    newPassword: string;
}

/** Same client rule as angular-old change-password form. */
export const PASSWORD_COMPLEXITY_PATTERN =
    /(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?!.*\s)[0-9a-zA-Z!@#$%^&*()]*$/;


export interface TenantDto {
    id: number;
    tenancyName: string;
    name: string;
    isActive: boolean;
}

export interface CreateTenantDto {
    tenancyName: string;
    name: string;
    adminEmailAddress: string;
    connectionString?: string;
    isActive: boolean;
}

export interface PagedTenantResultRequestDto {
    keyword?: string;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

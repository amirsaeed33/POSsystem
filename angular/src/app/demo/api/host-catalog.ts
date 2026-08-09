export const HostCatalogItemTypes = {
    CompanyType: 'CompanyType',
    Category: 'Category',
    Unit: 'Unit',
    Brand: 'Brand',
} as const;

export type HostCatalogItemType =
    (typeof HostCatalogItemTypes)[keyof typeof HostCatalogItemTypes];

export interface HostCatalogItemDto {
    id: number;
    type: string;
    companyTypeId?: number | null;
    companyTypeName?: string;
    name: string;
    symbol?: string;
    isActive: boolean;
}

export interface CreateHostCatalogItemDto {
    type: string;
    companyTypeId?: number | null;
    name: string;
    symbol?: string;
    isActive?: boolean;
}

export interface PagedHostCatalogItemResultRequestDto {
    keyword?: string;
    type?: string;
    companyTypeId?: number;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

export interface HostCatalogByCompanyTypeDto {
    companyType: HostCatalogItemDto;
    categories: HostCatalogItemDto[];
    units: HostCatalogItemDto[];
    brands: HostCatalogItemDto[];
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

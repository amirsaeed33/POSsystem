export interface BranchDto {
    id: number;
    name: string;
    code: string;
    isActive: boolean;
    isDefault: boolean;
    imagePath?: string;
    imageBase64?: string;
    invoiceAddress?: string;
    invoiceContactEmail?: string;
    invoiceContactPhone?: string;
    taxNumber?: string;
    website?: string;
    invoiceFooter?: string;
}

export interface CreateBranchDto {
    name: string;
    code: string;
    isActive?: boolean;
    isDefault?: boolean;
    imageBase64?: string;
    invoiceAddress?: string;
    invoiceContactEmail?: string;
    invoiceContactPhone?: string;
    taxNumber?: string;
    website?: string;
    invoiceFooter?: string;
}

export interface PagedBranchResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

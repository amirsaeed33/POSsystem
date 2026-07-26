export interface CompanyProfileDto {
    id: number;
    name: string;
    imagePath?: string;
    imageBase64?: string;
    invoiceAddress?: string;
    invoiceContactEmail?: string;
    invoiceContactPhone?: string;
    taxNumber?: string;
    website?: string;
    invoiceFooter?: string;
}

export interface CreateCompanyProfileDto {
    name: string;
    imageBase64?: string;
    invoiceAddress?: string;
    invoiceContactEmail?: string;
    invoiceContactPhone?: string;
    taxNumber?: string;
    website?: string;
    invoiceFooter?: string;
}

export interface PagedCompanyProfileResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

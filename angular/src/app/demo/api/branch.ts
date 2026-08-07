export const BranchStatuses = {
    Pending: 'Pending',
    Approved: 'Approved',
    Rejected: 'Rejected',
} as const;

export type BranchStatus = (typeof BranchStatuses)[keyof typeof BranchStatuses];

export interface BranchDto {
    id: number;
    name: string;
    code: string;
    statusId: number;
    /** LookUp.Name */
    status: string;
    statusDisplayName?: string;
    creationTime?: string;
    tenantId?: number | null;
    tenancyName?: string;
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
    taxPercent?: number;
    discountPercent?: number;
    discountAmount?: number;
}

export interface CreateBranchDto {
    name: string;
    code: string;
    isActive?: boolean;
    isDefault?: boolean;
    imageBase64?: string | null;
    invoiceAddress?: string;
    invoiceContactEmail?: string;
    invoiceContactPhone?: string;
    taxNumber?: string;
    website?: string;
    invoiceFooter?: string;
    taxPercent?: number;
    discountPercent?: number;
    discountAmount?: number;
}

export interface PagedBranchResultRequestDto {
    keyword?: string;
    statusId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

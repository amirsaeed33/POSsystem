export const SellerStatuses = {
    Pending: 0,
    Approved: 1,
    Rejected: 2,
} as const;

export type SellerStatus = (typeof SellerStatuses)[keyof typeof SellerStatuses];

export interface SellerDto {
    id?: number;
    fullName: string;
    email?: string;
    phoneNumber: string;
    city?: string;
    cnic?: string;
    companyName?: string;
    experienceNotes?: string;
    message?: string;
    status: SellerStatus;
    statusName?: string;
    isPubliclyListed?: boolean;
    commissionPerSalePkr?: number;
    creationTime?: string | Date;
    reviewedTime?: string | Date | null;
    reviewNotes?: string;
    rejectionReason?: string;
}

export interface PublicSellerDto {
    id?: number;
    fullName: string;
    city?: string;
    phoneNumber?: string;
    companyName?: string;
    commissionPerSalePkr?: number;
}

export interface RegisterSellerDto {
    fullName: string;
    email?: string;
    phoneNumber: string;
    city?: string;
    cnic?: string;
    companyName?: string;
    experienceNotes?: string;
    message?: string;
}

export interface RejectSellerDto {
    id: number;
    rejectionReason?: string;
}

export interface PagedSellerResultRequestDto {
    keyword?: string;
    status?: SellerStatus | null;
    skipCount?: number;
    maxResultCount?: number;
    sorting?: string;
}

export interface PackageDto {
    id?: number;
    name: string;
    price: string;
    period?: string;
    blurb?: string;
    features?: string;
    ctaLabel?: string;
    isFeatured?: boolean;
    contactViaWhatsApp?: boolean;
    isActive?: boolean;
    sortOrder?: number;
    monthlyAmount?: number;
    yearlyAmount?: number;
}

export interface CreatePackageDto {
    name: string;
    price: string;
    period?: string;
    blurb?: string;
    features?: string;
    ctaLabel?: string;
    isFeatured?: boolean;
    contactViaWhatsApp?: boolean;
    isActive?: boolean;
    sortOrder?: number;
    monthlyAmount?: number;
    yearlyAmount?: number;
}

export interface PagedPackageResultRequestDto {
    keyword?: string;
    isActive?: boolean | null;
    skipCount?: number;
    maxResultCount?: number;
    sorting?: string;
}

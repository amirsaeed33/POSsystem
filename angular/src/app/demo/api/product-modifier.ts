export interface ProductModifierGroupDto {
    id: number;
    productId: number;
    name: string;
    minSelection: number;
    maxSelection: number;
    isRequired: boolean;
    allowMultiple?: boolean;
    options: ProductModifierOptionDto[];
}

export interface ProductModifierOptionDto {
    id: number;
    modifierGroupId: number;
    name: string;
    price: number;
    extraPrice?: number;
    /** Empty price box on create — display sale, do not add as +extra. */
    isIncludedInBasePrice?: boolean;
    isDefault: boolean;
}

export interface CreateModifierGroupDto {
    productId: number;
    name: string;
    minSelection?: number;
    maxSelection?: number;
    isRequired?: boolean;
    allowMultiple?: boolean;
    options?: CreateModifierOptionDto[];
}

export interface CreateModifierOptionDto {
    name: string;
    price?: number;
    extraPrice?: number;
    /** True only when admin left the price field empty. */
    isIncludedInBasePrice?: boolean;
    isDefault?: boolean;
}

export interface UpdateModifierOptionDto {
    id: number;
    name: string;
    price?: number;
    isIncludedInBasePrice?: boolean;
    isDefault?: boolean;
}

export interface BranchModifierTemplateOptionDto {
    name: string;
    /** First/default size: uses product sale price (included). */
    useSalePrice: boolean;
    /** Add-on amount when useSalePrice is false. */
    price: number;
    isDefault?: boolean;
}

export interface BranchModifierTemplateDto {
    branchId: number;
    groupName: string;
    isRequired: boolean;
    allowMultiple: boolean;
    options: BranchModifierTemplateOptionDto[];
}

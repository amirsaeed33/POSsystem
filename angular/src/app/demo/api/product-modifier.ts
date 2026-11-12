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

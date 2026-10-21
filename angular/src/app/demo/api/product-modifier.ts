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
    isDefault?: boolean;
}

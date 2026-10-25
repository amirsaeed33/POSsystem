export interface PosCartLine {
    productId: number;
    productName: string;
    quantity: number;
    /** Locked catalog price for current customer type (retail or wholesale). */
    unitPrice: number;
    retailPrice: number;
    wholesalePrice: number;
    stockQuantity: number;
    id?: number;
    kitchenNotes?: string;
    selectedModifiers?: string;
    isSentToKitchen?: boolean;
    isDeal?: boolean;
    dealId?: number;
    dealSummary?: string;
}

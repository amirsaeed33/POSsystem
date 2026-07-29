export interface PosCartLine {
    productId: number;
    productName: string;
    quantity: number;
    /** Locked catalog price for current customer type (retail or wholesale). */
    unitPrice: number;
    retailPrice: number;
    wholesalePrice: number;
    stockQuantity: number;
}

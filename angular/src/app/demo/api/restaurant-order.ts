export const OrderTypes = {
    DineIn: 1,
    Takeaway: 2,
    Delivery: 3,
} as const;

export type OrderType = (typeof OrderTypes)[keyof typeof OrderTypes];

export const RestaurantOrderStatuses = {
    Open: 0,
    Billed: 1,
    Settled: 2,
    Cancelled: 3,
} as const;

export type RestaurantOrderStatus =
    (typeof RestaurantOrderStatuses)[keyof typeof RestaurantOrderStatuses];

export interface RestaurantOrderDto {
    id: number;
    branchId: number;
    orderNo: string;
    tableId?: number | null;
    tableNo?: string;
    orderType: OrderType;
    orderTypeName: string;
    status: RestaurantOrderStatus;
    statusName: string;
    customerId?: number | null;
    customerName?: string;
    waiterName?: string;
    notes?: string;
    subTotal: number;
    discountPercent: number;
    discountAmount: number;
    taxPercent: number;
    taxAmount: number;
    serviceChargePercent: number;
    serviceChargeAmount: number;
    grandTotal: number;
    saleId?: number | null;
    creationTime: string;
    billedTime?: string | null;
    settledTime?: string | null;
    lastPrintedBy?: string;
    lastPrintedTime?: string | null;
    lines: RestaurantOrderLineDto[];
}

export interface RestaurantOrderLineDto {
    id: number;
    orderId: number;
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    kitchenNotes?: string;
    selectedModifiers?: string;
    isSentToKitchen: boolean;
    sentToKitchenTime?: string | null;
}

export interface CreateOrUpdateRestaurantOrderDto {
    id?: number | null;
    branchId: number;
    tableId?: number | null;
    tableNo?: string;
    orderType: OrderType;
    customerId?: number | null;
    waiterName?: string;
    notes?: string;
    discountPercent?: number;
    discountAmount?: number;
    taxPercent?: number;
    serviceChargePercent?: number;
    lines: CreateOrUpdateRestaurantOrderLineDto[];
}

export interface CreateOrUpdateRestaurantOrderLineDto {
    id?: number | null;
    productId: number;
    productName?: string;
    quantity: number;
    unitPrice: number;
    kitchenNotes?: string;
    selectedModifiers?: string;
    isSentToKitchen?: boolean;
}

export interface SendToKitchenResultDto {
    orderId: number;
    orderNo: string;
    tableNo: string;
    waiterName: string;
    printTime: string;
    printedBy?: string;
    printedByUserId?: number | null;
    printedByName?: string;
    printedByRole?: string;
    newItems: KitchenSlipItemDto[];
}

export interface KitchenSlipItemDto {
    productName: string;
    quantity: number;
    kitchenNotes?: string;
    selectedModifiers?: string;
}

export interface SettleRestaurantOrderDto {
    orderId: number;
    customerId: number;
    paymentType: number;
    cashAmount: number;
    cardAmount: number;
    discountAmount?: number;
    discountPercent?: number;
    taxPercent?: number;
    serviceChargePercent?: number;
    notes?: string;
}

export interface SplitBillDto {
    sourceOrderId: number;
    numberOfSplits?: number;
    splitEqually?: boolean;
    moveLineIds?: number[];
}

export interface RecordPrintLogDto {
    branchId: number;
    orderId?: number | null;
    orderNo?: string;
    receiptType: string;
}

export interface RestaurantPrintLogDto {
    id: number;
    branchId: number;
    orderId?: number | null;
    orderNo?: string;
    receiptType: string;
    printedByUserId: number;
    printedByName?: string;
    printedByRole?: string;
    printTime: string;
}

export interface UnpaidTakeawayOrderDto {
    id: number;
    orderNo: string;
    creationTime: string;
    status: string;
    grandTotal: number;
    waiterName?: string;
    customerName?: string;
    itemCount: number;
}

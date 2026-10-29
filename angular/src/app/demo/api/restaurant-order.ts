export const OrderTypes = {
    DineIn: 1,
    Takeaway: 2,
    Delivery: 3,
    CarService: 4,
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
    carName?: string;
    carNumber?: string;
    carLocation?: string;
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
    printedQuantity?: number;
    sentToKitchenTime?: string | null;
}

export interface CreateOrUpdateRestaurantOrderDto {
    id?: number | null;
    branchId: number;
    tableId?: number | null;
    tableNo?: string;
    orderType: OrderType;
    carName?: string;
    carNumber?: string;
    carLocation?: string;
    customerId?: number | null;
    waiterName?: string;
    notes?: string;
    discountPercent?: number;
    discountAmount?: number;
    taxPercent?: number;
    serviceChargePercent?: number;
    lines: CreateOrUpdateRestaurantOrderLineDto[];
}

export interface ReserveCarServiceDto {
    branchId: number;
    tableId?: number | null;
    carName: string;
    carNumber: string;
    carLocation: string;
    waiterName?: string;
    notes?: string;
}

export interface OpenTableSessionDto {
    branchId: number;
    tableId: number;
    waiterName?: string;
    carNumber?: string;
    notes?: string;
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
    printedQuantity?: number;
}

export interface SendToKitchenResultDto {
    orderId: number;
    orderNo: string;
    tableNo: string;
    carName?: string;
    carNumber?: string;
    carLocation?: string;
    waiterName: string;
    printTime: string;
    printedBy?: string;
    printedByUserId?: number | null;
    printedByName?: string;
    printedByRole?: string;
    hasNewItems?: boolean;
    message?: string;
    newItems: KitchenSlipItemDto[];
}

export interface KitchenSlipItemDto {
    lineId?: number;
    productName: string;
    quantity: number;
    previousQuantity?: number;
    totalQuantity?: number;
    kitchenNotes?: string;
    selectedModifiers?: string;
}

export interface ConfirmKotPrintDto {
    orderId: number;
    lineIds?: number[];
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

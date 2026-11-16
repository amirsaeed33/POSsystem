export enum PaymentType {
    Cash = 0,
    Card = 1,
    Credit = 2,
    Mixed = 3,
}

export interface SaleLineDto {
    id: number;
    saleId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    isDeal?: boolean;
    dealItems?: string[];
}

export interface SalePaymentHistoryItemDto {
    paymentDate: string | Date;
    amount: number;
    method: string;
    description?: string;
    isAdditionalCollection?: boolean;
}

export interface SaleDto {
    id: number;
    customerId: number;
    customerName?: string;
    saleDate: string | Date;
    invoiceNo?: string;
    tokenNo?: number | null;
    subTotal: number;
    discountAmount: number;
    discountPercent: number;
    taxPercent: number;
    taxAmount: number;
    totalAmount: number;
    paymentType: number;
    cashAmount: number;
    cardAmount: number;
    creditAmount: number;
    paidAmount?: number;
    dueAmount?: number;
    notes?: string;
    saleType?: string;
    deliveryCharges?: number;
    deliveryAreaName?: string;
    hasReturns?: boolean;
    returnCount?: number;
    paymentHistory?: SalePaymentHistoryItemDto[];
    lines?: SaleLineDto[];
}

export interface CreateSaleLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;
}

export interface CreateSaleDto {
    customerId: number;
    saleDate: string | Date;
    invoiceNo?: string;
    tokenNo?: number | null;
    notes?: string;
    saleType?: string;
    deliveryCharges?: number;
    deliveryAreaName?: string;
    discountAmount: number;
    discountPercent: number;
    taxPercent: number;
    paymentType: number;
    cashAmount: number;
    cardAmount: number;
    lines: CreateSaleLineDto[];
}

export interface PagedSaleResultRequestDto {
    keyword?: string;
    customerId?: number;
    fromDate?: string;
    toDate?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface CollectSalePaymentDto {
    saleId: number;
    amount: number;
    paymentMethod: number; // 1 = Cash, 2 = Card
    notes?: string;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

export interface GetDepartmentSalesInput {
    departmentId?: number;
    keyword?: string;
    fromDate?: string;
    toDate?: string;
}

export interface DepartmentSalesReportDto {
    departments: DepartmentSalesGroupDto[];
    grandTotalAmount: number;
    grandTotalQuantity: number;
    totalDepartmentsCount: number;
    totalProductsSold: number;
}

export interface DepartmentSalesGroupDto {
    departmentId?: number;
    departmentName: string;
    totalSalesAmount: number;
    totalQuantitySold: number;
    products: DepartmentProductSaleDto[];
}

export interface DepartmentProductSaleDto {
    productId: number;
    productName: string;
    price: number;
    quantitySold: number;
    totalAmount: number;
    categoryId?: number;
    categoryName?: string;
    departmentId?: number;
    departmentName?: string;
}

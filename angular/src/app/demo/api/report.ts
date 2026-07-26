export interface ReportDateRangeInput {
    fromDate?: string | Date;
    toDate?: string | Date;
    keyword?: string;
}

export interface SaleReportRowDto {
    id: number;
    invoiceNo?: string;
    saleDate: string | Date;
    customerName?: string;
    totalAmount: number;
    notes?: string;
}

export interface SaleReportDto {
    totalAmount: number;
    items: SaleReportRowDto[];
}

export interface PurchaseReportRowDto {
    id: number;
    invoiceNo?: string;
    purchaseDate: string | Date;
    supplierName?: string;
    totalAmount: number;
    notes?: string;
}

export interface PurchaseReportDto {
    totalAmount: number;
    items: PurchaseReportRowDto[];
}

export interface ExpenseReportRowDto {
    id: number;
    expenseDate: string | Date;
    referenceNo?: string;
    description?: string;
    paymentAccountName?: string;
    amount: number;
}

export interface ExpenseReportDto {
    totalAmount: number;
    items: ExpenseReportRowDto[];
}

export interface StockReportRowDto {
    id: number;
    name?: string;
    barcode?: string;
    categoryName?: string;
    brandName?: string;
    unitName?: string;
    price: number;
    costPrice: number;
    profitPerUnit: number;
    profitMarginPercent?: number | null;
    stockProfit: number;
    stockQuantity: number;
    alertQuantityLimit: number;
    status?: string;
}

export interface StockReportDto {
    totalProducts: number;
    inStockCount: number;
    lowStockCount: number;
    outOfStockCount: number;
    totalStockUnits: number;
    totalStockCostValue: number;
    totalStockSellValue: number;
    totalStockProfit: number;
    items: StockReportRowDto[];
}

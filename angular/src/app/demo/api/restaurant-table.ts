export const TableStatuses = {
    Available: 0,
    Occupied: 1,
    Billed: 2,
    Reserved: 3,
} as const;

export type TableStatus = (typeof TableStatuses)[keyof typeof TableStatuses];

export interface RestaurantTableDto {
    id: number;
    branchId: number;
    tableNo: string;
    area?: string;
    capacity: number;
    status: TableStatus;
    statusName: string;
    currentOrderId?: number | null;
    isActive: boolean;
}

export interface CreateRestaurantTableDto {
    branchId: number;
    tableNo: string;
    area?: string;
    capacity: number;
    isActive?: boolean;
}

export interface ChangeTableStatusDto {
    tableId: number;
    status: TableStatus;
    currentOrderId?: number | null;
}

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
    waiterId?: number | null;
    waiterName?: string;
    isActive: boolean;
}

export interface AssignWaiterDto {
    tableId: number;
    waiterId?: number | null;
    waiterName?: string;
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

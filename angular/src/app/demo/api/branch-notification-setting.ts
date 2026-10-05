export interface BranchNotificationSettingDto {
    id: number;
    tenantId?: number | null;
    branchId: number;
    isEnabled: boolean;
    notifyOnSale: boolean;
    notifyOnPurchase: boolean;
    notifyOnExpense: boolean;
    emails: string;
    emailList: string[];
}

export interface CreateOrUpdateBranchNotificationSettingDto {
    branchId: number;
    isEnabled: boolean;
    notifyOnSale: boolean;
    notifyOnPurchase: boolean;
    notifyOnExpense: boolean;
    emails?: string;
    emailList?: string[];
}

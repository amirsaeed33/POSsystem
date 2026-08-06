export enum TenantAvailabilityState {
    Available = 1,
    InActive = 2,
    NotFound = 3,
}

export interface IsTenantAvailableInput {
    tenancyName: string;
}

export interface IsTenantAvailableOutput {
    state: TenantAvailabilityState;
    tenantId?: number;
}

export interface SignUpTenantInput {
    tenancyName: string;
    name: string;
    adminName: string;
    adminSurname: string;
    adminEmailAddress: string;
    adminUserName: string;
    adminPassword: string;
}

export interface SignUpTenantOutput {
    tenantId: number;
    tenancyName: string;
    name: string;
    adminUserName: string;
    canLogin: boolean;
}

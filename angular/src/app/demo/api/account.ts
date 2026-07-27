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

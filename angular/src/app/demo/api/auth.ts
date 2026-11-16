export interface AuthenticateModel {
    userNameOrEmailAddress: string;
    password: string;
    rememberClient?: boolean;
    latitude?: number | null;
    longitude?: number | null;
}

export interface AuthenticateResultModel {
    accessToken: string;
    encryptedAccessToken: string;
    expireInSeconds: number;
    userId: number;
    tenantId?: number | null;
    tenancyName?: string | null;
    attendanceAutoCheckedIn?: boolean;
}


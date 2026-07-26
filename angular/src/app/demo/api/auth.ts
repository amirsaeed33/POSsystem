export interface AuthenticateModel {
    userNameOrEmailAddress: string;
    password: string;
    rememberClient?: boolean;
}

export interface AuthenticateResultModel {
    accessToken: string;
    encryptedAccessToken: string;
    expireInSeconds: number;
    userId: number;
}


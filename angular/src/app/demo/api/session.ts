export interface UserLoginInfoDto {
    id: number;
    name: string;
    surname: string;
    userName: string;
    emailAddress: string;
    roleNames?: string[];
    profilePictureUrl?: string;
}

export interface TenantLoginInfoDto {
    id: number;
    tenancyName: string;
    name: string;
}

export interface ApplicationInfoDto {
    version: string;
    releaseDate: string;
    features: { [key: string]: boolean };
}

export interface GetCurrentLoginInformationsOutput {
    application: ApplicationInfoDto;
    user: UserLoginInfoDto;
    tenant: TenantLoginInfoDto;
}


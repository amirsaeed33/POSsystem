export interface DeveloperDto {
    id?: number;
    name: string;
    email?: string;
    phoneNumber?: string;
    description?: string;
    profileImagePath?: string;
    backgroundImagePath?: string;
    education?: string;
    tags?: string;
    isActive?: boolean;
    sortOrder?: number;
    profileImageBase64?: string | null;
    backgroundImageBase64?: string | null;
    clearProfileImage?: boolean;
    clearBackgroundImage?: boolean;
}

export interface CreateDeveloperDto {
    name: string;
    email?: string;
    phoneNumber?: string;
    description?: string;
    education?: string;
    tags?: string;
    isActive?: boolean;
    sortOrder?: number;
    profileImageBase64?: string | null;
    backgroundImageBase64?: string | null;
}

export interface PagedDeveloperResultRequestDto {
    keyword?: string;
    isActive?: boolean | null;
    skipCount?: number;
    maxResultCount?: number;
    sorting?: string;
}

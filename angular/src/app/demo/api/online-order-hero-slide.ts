export interface OnlineOrderHeroSlideDto {
    id: number;
    branchId: number;
    sortOrder: number;
    isActive: boolean;
    badge?: string;
    badgeIcon?: string;
    titleMain: string;
    titleHighlight?: string;
    titleEnd?: string;
    description?: string;
    offerPill?: string;
    rating?: string;
    primaryBtnText?: string;
    primaryTarget?: string;
    secondaryBtnText?: string;
    secondaryTarget?: string;
    imagePath?: string;
    bgImagePath?: string;
    imageBase64?: string;
    bgImageBase64?: string;
}

export interface CreateOrUpdateOnlineOrderHeroSlideDto {
    id?: number | null;
    branchId: number;
    sortOrder: number;
    isActive: boolean;
    badge?: string;
    badgeIcon?: string;
    titleMain: string;
    titleHighlight?: string;
    titleEnd?: string;
    description?: string;
    offerPill?: string;
    rating?: string;
    primaryBtnText?: string;
    primaryTarget?: string;
    secondaryBtnText?: string;
    secondaryTarget?: string;
    imagePath?: string;
    bgImagePath?: string;
    imageBase64?: string;
    bgImageBase64?: string;
}

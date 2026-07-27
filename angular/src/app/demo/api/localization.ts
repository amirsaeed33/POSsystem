export interface LanguageInfo {
    name: string;
    displayName: string;
    icon?: string;
    isDisabled?: boolean;
    isDefault?: boolean;
}

export interface ChangeUserLanguageDto {
    languageName: string;
}

/** Seeded in backend DefaultLanguagesCreator — used only as fallback. */
export const DEFAULT_LANGUAGES: LanguageInfo[] = [
    { name: 'en', displayName: 'English', icon: 'famfamfam-flags us' },
    { name: 'ar', displayName: 'العربية', icon: 'famfamfam-flags sa' },
    { name: 'de', displayName: 'German', icon: 'famfamfam-flags de' },
    { name: 'it', displayName: 'Italiano', icon: 'famfamfam-flags it' },
    { name: 'fa', displayName: 'فارسی', icon: 'famfamfam-flags ir' },
    { name: 'fr', displayName: 'Français', icon: 'famfamfam-flags fr' },
    { name: 'pt-BR', displayName: 'Português', icon: 'famfamfam-flags br' },
    { name: 'tr', displayName: 'Türkçe', icon: 'famfamfam-flags tr' },
    { name: 'ru', displayName: 'Русский', icon: 'famfamfam-flags ru' },
    { name: 'zh-Hans', displayName: '简体中文', icon: 'famfamfam-flags cn' },
    { name: 'es-MX', displayName: 'Español México', icon: 'famfamfam-flags mx' },
    { name: 'nl', displayName: 'Nederlands', icon: 'famfamfam-flags nl' },
    { name: 'ja', displayName: '日本語', icon: 'famfamfam-flags jp' },
];

export const DEFAULT_LANGUAGE_NAME = 'en';
export const LOCALIZATION_CULTURE_COOKIE = 'Abp.Localization.CultureName';

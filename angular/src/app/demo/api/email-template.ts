export interface EmailTemplateDto {
    id: number;
    name: string;
    code: string;
    subject: string;
    bodyHtml: string;
    description?: string;
    isActive: boolean;
}

export interface CreateEmailTemplateDto {
    name: string;
    code: string;
    subject: string;
    bodyHtml: string;
    description?: string;
    isActive: boolean;
}

export interface PagedEmailTemplateResultRequestDto {
    keyword?: string;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PreviewEmailTemplateInput {
    subject: string;
    bodyHtml: string;
    sampleValues?: Record<string, string>;
}

export interface PreviewEmailTemplateOutput {
    subject: string;
    bodyHtml: string;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}

export const EMAIL_TEMPLATE_SAMPLE_VALUES: Record<string, string> = {
    Code: '123456',
    ExpirationMinutes: '5',
    UserName: 'admin',
    Name: 'Admin User',
    Email: 'admin@example.com',
    AppName: 'SmartPos',
};

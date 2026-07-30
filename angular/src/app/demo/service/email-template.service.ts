import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateEmailTemplateDto,
    EmailTemplateDto,
    PagedEmailTemplateResultRequestDto,
    PagedResultDto,
    PreviewEmailTemplateInput,
    PreviewEmailTemplateOutput,
} from '../api/email-template';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class EmailTemplateService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/EmailTemplate`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedEmailTemplateResultRequestDto
    ): Promise<PagedResultDto<EmailTemplateDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.isActive !== undefined) {
            params.IsActive = input.isActive;
        }
        if (input?.skipCount !== undefined) {
            params.SkipCount = input.skipCount;
        }
        if (input?.maxResultCount !== undefined) {
            params.MaxResultCount = input.maxResultCount;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load email templates');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapTemplate(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<EmailTemplateDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapTemplate(this.unwrap(res, 'Failed to load email template'));
    }

    async create(input: CreateEmailTemplateDto): Promise<EmailTemplateDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.mapTemplate(this.unwrap(res, 'Failed to create email template'));
    }

    async update(input: EmailTemplateDto): Promise<EmailTemplateDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, input)
        );
        return this.mapTemplate(this.unwrap(res, 'Failed to update email template'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete email template');
    }

    async preview(input: PreviewEmailTemplateInput): Promise<PreviewEmailTemplateOutput> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Preview`, input)
        );
        const result = this.unwrap(res, 'Failed to preview email template');
        return {
            subject: result.subject ?? result.Subject ?? '',
            bodyHtml: result.bodyHtml ?? result.BodyHtml ?? '',
        };
    }

    /** Client-side placeholder render for instant live preview. */
    renderLocal(template: string, values: Record<string, string>): string {
        if (!template) {
            return '';
        }
        let result = template;
        Object.keys(values || {}).forEach((key) => {
            const token = new RegExp(`\\{\\{\\s*${key}\\s*\\}\\}`, 'gi');
            result = result.replace(token, values[key] ?? '');
        });
        return result;
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }
        return res.result ?? res;
    }

    private mapTemplate(item: any): EmailTemplateDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            code: item.code ?? item.Code,
            subject: item.subject ?? item.Subject,
            bodyHtml: item.bodyHtml ?? item.BodyHtml ?? '',
            description: item.description ?? item.Description,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}

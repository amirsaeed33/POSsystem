import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MessageService } from 'primeng/api';
import {
    CreateEmailTemplateDto,
    EMAIL_TEMPLATE_SAMPLE_VALUES,
    EmailTemplateDto,
} from 'src/app/demo/api/email-template';
import { EmailTemplateService } from 'src/app/demo/service/email-template.service';

@Component({
    selector: 'app-email-template-form-dialog',
    templateUrl: './email-template-form-dialog.component.html',
    styleUrls: ['./email-template-form-dialog.component.scss'],
})
export class EmailTemplateFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() templateId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    template: EmailTemplateDto = this.emptyTemplate();
    saving = false;
    loading = false;
    sampleValues = { ...EMAIL_TEMPLATE_SAMPLE_VALUES };

    constructor(
        private emailTemplateService: EmailTemplateService,
        private messageService: MessageService,
        private sanitizer: DomSanitizer
    ) {}

    get dialogTitle(): string {
        return this.templateId ? 'Edit Email Template' : 'Create Email Template';
    }

    get previewSubject(): string {
        return this.emailTemplateService.renderLocal(
            this.template.subject || '',
            this.sampleValues
        );
    }

    get previewBodySafeHtml(): SafeHtml {
        const html = this.emailTemplateService.renderLocal(
            this.template.bodyHtml || '',
            this.sampleValues
        );
        return this.sanitizer.bypassSecurityTrustHtml(html);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.templateId) {
                this.loadTemplate(this.templateId);
            }
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    save(): void {
        const name = (this.template.name || '').trim();
        const code = (this.template.code || '').trim().replace(/\s+/g, '');
        const subject = (this.template.subject || '').trim();
        const bodyHtml = (this.template.bodyHtml || '').trim();

        if (!name || !code || !subject || !bodyHtml) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name, code, subject, and HTML body are required.',
            });
            return;
        }

        this.saving = true;
        const request = this.templateId
            ? this.emailTemplateService.update({
                  ...this.template,
                  id: this.templateId,
                  name,
                  code,
                  subject,
                  bodyHtml,
                  description: this.template.description?.trim() || undefined,
              })
            : this.emailTemplateService.create({
                  name,
                  code,
                  subject,
                  bodyHtml,
                  description: this.template.description?.trim() || undefined,
                  isActive: this.template.isActive,
              } as CreateEmailTemplateDto);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.templateId
                        ? 'Email template updated successfully'
                        : 'Email template created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save email template',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyTemplate(): EmailTemplateDto {
        return {
            id: 0,
            name: '',
            code: '',
            subject: '',
            bodyHtml: '',
            description: '',
            isActive: true,
        };
    }

    private resetForm(): void {
        this.template = this.emptyTemplate();
        this.saving = false;
        this.loading = false;
    }

    private loadTemplate(id: number): void {
        this.loading = true;
        this.emailTemplateService
            .get(id)
            .then((template) => {
                this.template = { ...template };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load email template',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

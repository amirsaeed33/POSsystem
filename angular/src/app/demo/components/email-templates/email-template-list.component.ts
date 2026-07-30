import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EmailTemplateDto } from 'src/app/demo/api/email-template';
import { EmailTemplateService } from 'src/app/demo/service/email-template.service';

@Component({
    templateUrl: './email-template-list.component.html',
    styleUrls: ['./email-template-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class EmailTemplateListComponent implements OnInit {
    templates: EmailTemplateDto[] = [];
    loading = false;
    keyword = '';

    dialogVisible = false;
    editingId: number | null = null;

    previewVisible = false;
    previewSubject = '';
    previewBodySafeHtml: SafeHtml = '';

    constructor(
        private emailTemplateService: EmailTemplateService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private sanitizer: DomSanitizer
    ) {}

    ngOnInit(): void {
        this.loadTemplates();
    }

    loadTemplates(): void {
        this.loading = true;
        this.emailTemplateService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.templates = result.items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load email templates',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadTemplates();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingId = null;
        this.dialogVisible = true;
    }

    openEditDialog(template: EmailTemplateDto): void {
        this.editingId = template.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadTemplates();
    }

    openPreview(template: EmailTemplateDto): void {
        this.emailTemplateService
            .preview({
                subject: template.subject,
                bodyHtml: template.bodyHtml,
            })
            .then((result) => {
                this.previewSubject = result.subject;
                this.previewBodySafeHtml = this.sanitizer.bypassSecurityTrustHtml(
                    result.bodyHtml || ''
                );
                this.previewVisible = true;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to preview template',
                });
            });
    }

    onDelete(template: EmailTemplateDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete template "${template.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.emailTemplateService
                    .delete(template.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Email template deleted successfully',
                        });
                        this.loadTemplates();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete email template',
                        });
                    });
            },
        });
    }
}

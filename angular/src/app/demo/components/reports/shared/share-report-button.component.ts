import { Component, Input, Optional } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { MessageService } from 'primeng/api';
import { Observable, firstValueFrom } from 'rxjs';
import { ShareService } from 'src/app/demo/service/share.service';

@Component({
    selector: 'app-share-report-button',
    standalone: true,
    imports: [CommonModule, ButtonModule, RippleModule],
    template: `
        <button
            pButton
            pRipple
            type="button"
            [icon]="icon"
            [label]="label"
            [class]="styleClass"
            [loading]="loading"
            [disabled]="loading"
            (click)="onShare()"
        ></button>
    `,
})
export class ShareReportButtonComponent {
    @Input() fetchPdf!: () => Observable<Blob>;
    @Input() fileName: string = 'Report.pdf';
    @Input() title: string = 'Report';
    @Input() label: string = 'Share';
    @Input() icon: string = 'pi pi-share-alt';
    @Input() styleClass: string = 'p-button-outlined p-button-success';

    loading = false;

    constructor(
        private shareService: ShareService,
        @Optional() private messageService?: MessageService
    ) {}

    async onShare(): Promise<void> {
        if (!this.fetchPdf || this.loading) {
            return;
        }

        this.loading = true;
        try {
            const blob = await firstValueFrom(this.fetchPdf());
            await this.shareService.sharePdf(
                blob,
                this.fileName,
                this.title,
                this.messageService
            );
        } catch (err: any) {
            if (err?.name !== 'AbortError') {
                const message =
                    err?.message || 'Failed to generate or share report PDF';
                if (this.messageService) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Share Error',
                        detail: message,
                    });
                } else {
                    console.error('Share error:', err);
                }
            }
        } finally {
            this.loading = false;
        }
    }
}

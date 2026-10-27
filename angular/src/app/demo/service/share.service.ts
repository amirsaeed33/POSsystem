import { Injectable, Optional } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({
    providedIn: 'root',
})
export class ShareService {
    constructor(@Optional() private rootMessageService?: MessageService) {}

    async sharePdf(
        blob: Blob,
        fileName: string,
        title: string,
        messageService?: MessageService
    ): Promise<{ shared: boolean; fallback: boolean }> {
        const file = new File([blob], fileName, { type: 'application/pdf' });

        // Check if Web Share API is available and can share this file
        if (
            typeof navigator !== 'undefined' &&
            navigator.canShare &&
            navigator.canShare({ files: [file] })
        ) {
            try {
                await navigator.share({
                    files: [file],
                    title: title,
                });
                return { shared: true, fallback: false };
            } catch (err: any) {
                // User cancelled the share dialog - do not treat as error
                if (err?.name === 'AbortError') {
                    return { shared: false, fallback: false };
                }
                throw err;
            }
        }

        // Fallback for browsers / devices without file sharing support (e.g. Desktop browsers):
        // 1. Download the generated PDF locally
        this.downloadBlob(blob, fileName);

        // 2. Show toast notifying user the PDF was downloaded and can be shared manually
        const toast = messageService || this.rootMessageService;
        if (toast) {
            toast.add({
                severity: 'info',
                summary: 'PDF Downloaded',
                detail: 'PDF downloaded. You can now share it from your device.',
            });
        }

        return { shared: false, fallback: true };
    }

    private downloadBlob(blob: Blob, fileName: string): void {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        setTimeout(() => {
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        }, 200);
    }
}

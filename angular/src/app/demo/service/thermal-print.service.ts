import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { CustomerOrderDto } from '../api/customer-order';
import { BranchContextService } from './branch-context.service';
import { BranchService } from './branch.service';
import { AuthService } from './auth.service';
import { BranchDto } from '../api/branch';
import { SaleDto } from '../api/sale';
import { PurchaseDto } from '../api/purchase';
import { SaleReturnDto } from '../api/sale-return';
import { PurchaseReturnDto } from '../api/purchase-return';
import { ExpenseDto } from '../api/expense';
import { LedgerEntryDto } from '../api/ledger-entry';

export type ThermalPaperSize = '58mm' | '80mm';

export interface ThermalPrintConfig {
    paperSize: ThermalPaperSize;
    autoPrint: boolean;
    copies: number;
    showLogo: boolean;
    showPreview: boolean;
    headerText: string;
    footerMessage: string;
}

const STORAGE_KEYS = {
    PAPER_SIZE: 'smartpos_thermal_paper_size',
    AUTO_PRINT: 'smartpos_thermal_auto_print',
    COPIES: 'smartpos_thermal_copies',
    SHOW_LOGO: 'smartpos_thermal_show_logo',
    SHOW_PREVIEW: 'smartpos_thermal_show_preview',
    HEADER_TEXT: 'smartpos_thermal_header_text',
    FOOTER_MESSAGE: 'smartpos_thermal_footer_message',
};

@Injectable({ providedIn: 'root' })
export class ThermalPrintService {
    private configSubject: BehaviorSubject<ThermalPrintConfig>;
    public config$: Observable<ThermalPrintConfig>;

    constructor(
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private authService: AuthService
    ) {
        const initialConfig = this.loadConfigFromStorage();
        this.configSubject = new BehaviorSubject<ThermalPrintConfig>(initialConfig);
        this.config$ = this.configSubject.asObservable();
    }

    public getConfig(): ThermalPrintConfig {
        return this.configSubject.getValue();
    }

    public updateConfig(partial: Partial<ThermalPrintConfig>): void {
        const updated: ThermalPrintConfig = {
            ...this.getConfig(),
            ...partial,
        };

        if (partial.paperSize !== undefined) {
            localStorage.setItem(STORAGE_KEYS.PAPER_SIZE, partial.paperSize);
        }
        if (partial.autoPrint !== undefined) {
            localStorage.setItem(STORAGE_KEYS.AUTO_PRINT, String(partial.autoPrint));
        }
        if (partial.copies !== undefined) {
            localStorage.setItem(STORAGE_KEYS.COPIES, String(partial.copies));
        }
        if (partial.showLogo !== undefined) {
            localStorage.setItem(STORAGE_KEYS.SHOW_LOGO, String(partial.showLogo));
        }
        if (partial.showPreview !== undefined) {
            localStorage.setItem(STORAGE_KEYS.SHOW_PREVIEW, String(partial.showPreview));
        }
        if (partial.headerText !== undefined) {
            localStorage.setItem(STORAGE_KEYS.HEADER_TEXT, partial.headerText);
        }
        if (partial.footerMessage !== undefined) {
            localStorage.setItem(STORAGE_KEYS.FOOTER_MESSAGE, partial.footerMessage);
        }

        this.configSubject.next(updated);
    }

    private loadConfigFromStorage(): ThermalPrintConfig {
        const rawSize = localStorage.getItem(STORAGE_KEYS.PAPER_SIZE);
        const paperSize: ThermalPaperSize = rawSize === '58mm' ? '58mm' : '80mm';

        const rawAutoPrint = localStorage.getItem(STORAGE_KEYS.AUTO_PRINT);
        const autoPrint = rawAutoPrint !== null ? rawAutoPrint === 'true' : true;

        const rawCopies = localStorage.getItem(STORAGE_KEYS.COPIES);
        const copies = rawCopies ? Math.max(1, Math.min(5, parseInt(rawCopies, 10) || 1)) : 1;

        const rawLogo = localStorage.getItem(STORAGE_KEYS.SHOW_LOGO);
        const showLogo = rawLogo !== null ? rawLogo === 'true' : true;

        const rawPreview = localStorage.getItem(STORAGE_KEYS.SHOW_PREVIEW);
        const showPreview = rawPreview !== null ? rawPreview === 'true' : false;

        const headerText = localStorage.getItem(STORAGE_KEYS.HEADER_TEXT) || '';
        const footerMessage =
            localStorage.getItem(STORAGE_KEYS.FOOTER_MESSAGE) ||
            'Thank you for your visit! Please come again.';

        return {
            paperSize,
            autoPrint,
            copies,
            showLogo,
            showPreview,
            headerText,
            footerMessage,
        };
    }

    // =========================================================================
    // Core CSS Stylesheet Generator for 58mm and 80mm
    // =========================================================================
    public getBaseThermalStyles(paperSize: ThermalPaperSize): string {
        const is58 = paperSize === '58mm';
        const bodyWidth = is58 ? '48mm' : '76mm';
        const baseFontSize = is58 ? '9.5px' : '12px';
        const titleFontSize = is58 ? '13px' : '16px';
        const subTitleFontSize = is58 ? '11px' : '14px';
        const metaFontSize = is58 ? '9px' : '11.5px';

        return `
            @page {
                margin: 0 !important;
            }
            @media print {
                html, body {
                    width: 100% !important;
                    max-width: ${bodyWidth} !important;
                    margin: 0 !important;
                    padding: 0 !important;
                    background: #ffffff !important;
                    color: #000000 !important;
                }
                .no-print { display: none !important; }
            }
            * {
                box-sizing: border-box;
                -webkit-print-color-adjust: exact !important;
                print-color-adjust: exact !important;
            }
            body {
                font-family: Arial, Helvetica, 'Segoe UI', sans-serif;
                font-size: ${baseFontSize};
                line-height: 1.34;
                color: #000000;
                background: #ffffff;
                width: 100%;
                max-width: ${bodyWidth};
                margin: 0 auto;
                padding: ${is58 ? '1.5mm 1mm' : '2.5mm 1.5mm'};
                font-weight: 400;
            }
            .text-center { text-align: center; }
            .text-right { text-align: right; }
            .text-left { text-align: left; }
            .bold { font-weight: 700; }
            .uppercase { text-transform: uppercase; }
            .divider { border-top: 1px dashed #000000; margin: 4px 0; }
            .double-divider { border-top: 1px dashed #000000; margin: 4px 0; }
            .dotted-divider { border-top: 1px dotted #000000; margin: 3px 0; }
            .row {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                gap: 6px;
                font-size: ${metaFontSize};
                margin-bottom: 2px;
                line-height: 1.3;
            }
            .row > span:last-child { text-align: right; }
            .printed-at {
                white-space: nowrap;
                flex: 0 0 auto;
                font-size: ${is58 ? '8.5px' : '10px'};
            }
            .header-title {
                font-size: ${titleFontSize};
                font-weight: 700;
                letter-spacing: 0.3px;
                margin-top: 2px;
            }
            .header-subtitle {
                font-size: ${subTitleFontSize};
                font-weight: 700;
                margin-top: 2px;
            }
            .meta-text {
                font-size: ${metaFontSize};
                color: #111111;
                line-height: 1.3;
            }
            .section-title {
                text-align: center;
                font-weight: 700;
                font-size: ${is58 ? '11px' : '13px'};
                letter-spacing: 0.3px;
                margin: 3px 0;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin: 2px 0 4px 0;
                table-layout: fixed;
                page-break-inside: avoid !important;
                break-inside: avoid !important;
            }
            tr, td, th {
                page-break-inside: avoid !important;
                break-inside: avoid !important;
            }
            .receipt-instance {
                page-break-inside: avoid !important;
                break-inside: avoid !important;
            }
            th {
                border-bottom: 1px dashed #000000;
                padding: 3px 2px;
                font-size: ${metaFontSize};
                font-weight: 700;
                word-break: break-word;
            }
            td {
                padding: 2px 2px;
                font-size: ${metaFontSize};
                vertical-align: top;
                word-break: break-word;
            }
            th:first-child, td:first-child {
                padding-left: 0;
            }
            th:last-child, td:last-child {
                padding-right: 0;
            }
            .logo-img {
                width: ${is58 ? '80%' : '76%'};
                max-width: ${is58 ? '48mm' : '70mm'};
                max-height: ${is58 ? '85px' : '115px'};
                height: auto;
                margin: 0 auto 3px auto;
                display: block;
                filter: grayscale(100%) contrast(140%);
                object-fit: contain;
            }
            .badge {
                display: inline-block;
                background: #000000;
                color: #ffffff;
                padding: 1px 3px;
                font-size: 8px;
                font-weight: 700;
                border-radius: 2px;
                margin-right: 2px;
            }
            .grand-total {
                font-size: ${is58 ? '11.5px' : '13px'};
                font-weight: 700;
                padding: 3px 0;
                margin: 2px 0 1px 0;
            }
            .label {
                font-weight: 700;
            }
            .customer-block {
                font-size: ${metaFontSize};
                line-height: 1.35;
            }
            .receipt-copy-separator {
                page-break-after: always;
                border-top: 2px dashed #000000;
                margin: 15px 0;
                padding-top: 6px;
                text-align: center;
                font-size: 9px;
            }
        `;
    }

    private getBranchInfo(overrideCompany?: BranchDto | null): {
        name: string;
        address: string;
        phone: string;
        email: string;
        taxNumber: string;
        footer: string;
        logoUrl: string;
        softwareDeveloperName?: string;
        softwareDeveloperPhone?: string;
    } {
        const branch = overrideCompany || this.branchContext.getCurrentBranch();
        const logoUrl = branch?.imagePath ? this.branchService.getImageUrl(branch.imagePath) : '';
        return {
            name: branch?.name || 'SMARTPOS RESTAURANT',
            address: branch?.invoiceAddress || '',
            phone: branch?.invoiceContactPhone || '',
            email: branch?.invoiceContactEmail || '',
            taxNumber: branch?.taxNumber || '',
            footer: branch?.invoiceFooter || this.getConfig().footerMessage || '',
            logoUrl: logoUrl,
            softwareDeveloperName: branch?.softwareDeveloperName || 'Software develop by AlNawabTech',
            softwareDeveloperPhone: branch?.softwareDeveloperPhone || '03167854606',
        };
    }

    // =========================================================================
    // Core Execution Method (multi-copy, popup or direct)
    // =========================================================================
    public executePrint(bodyHtml: string, title = 'Receipt', paperSize?: ThermalPaperSize): void {
        const config = this.getConfig();
        const effectiveSize = paperSize || config.paperSize;
        const copies = Math.max(1, config.copies || 1);

        let finalContent = '';
        for (let i = 1; i <= copies; i++) {
            const copyBadge =
                copies > 1
                    ? `<div class="text-center meta-text bold" style="margin-bottom:3px;">*** ${i === 1 ? 'CUSTOMER COPY' : i === 2 ? 'MERCHANT COPY' : 'COPY #' + i} ***</div>`
                    : '';
            finalContent += `<div class="receipt-instance">${copyBadge}${bodyHtml}</div>`;
            if (i < copies) {
                finalContent += `<div class="receipt-copy-separator">âœ‚ - - - - - - CUT HERE - - - - - - âœ‚</div>`;
            }
        }

        const fullHtml = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>${title}</title>
                <style>
                    ${this.getBaseThermalStyles(effectiveSize)}
                </style>
            </head>
            <body>
                ${finalContent}
            </body>
            </html>
        `;

        this.printViaIframe(fullHtml);
    }

    public printViaIframe(fullHtml: string): void {
        let printFrame = document.getElementById('smartpos-thermal-print-iframe') as HTMLIFrameElement;
        if (!printFrame) {
            printFrame = document.createElement('iframe');
            printFrame.id = 'smartpos-thermal-print-iframe';
            printFrame.style.position = 'fixed';
            printFrame.style.right = '0';
            printFrame.style.bottom = '0';
            printFrame.style.width = '0';
            printFrame.style.height = '0';
            printFrame.style.border = '0';
            printFrame.style.opacity = '0';
            printFrame.style.pointerEvents = 'none';
            printFrame.style.zIndex = '-99999';
            document.body.appendChild(printFrame);
        }

        const frameDoc = printFrame.contentWindow?.document || printFrame.contentDocument;
        if (!frameDoc) {
            console.error('Thermal print: Could not access iframe document');
            return;
        }

        frameDoc.open();
        frameDoc.write(fullHtml);
        frameDoc.close();

        // Guard: logo/img onload + fallback timeout previously both called print() â†’ duplicate invoices.
        let printed = false;
        let fallbackTimer: ReturnType<typeof setTimeout> | null = null;
        const triggerPrint = () => {
            if (printed) {
                return;
            }
            printed = true;
            if (fallbackTimer != null) {
                clearTimeout(fallbackTimer);
                fallbackTimer = null;
            }
            try {
                printFrame.contentWindow?.focus();
                printFrame.contentWindow?.print();
            } catch (err) {
                console.error('Thermal print execution failed:', err);
            }
        };

        const win = printFrame.contentWindow;
        if (win && frameDoc.images && frameDoc.images.length > 0) {
            let loaded = 0;
            const total = frameDoc.images.length;
            const onImgDone = () => {
                loaded++;
                if (loaded >= total) {
                    setTimeout(triggerPrint, 150);
                }
            };
            for (let i = 0; i < total; i++) {
                const img = frameDoc.images[i];
                if (img.complete) {
                    onImgDone();
                } else {
                    img.onload = onImgDone;
                    img.onerror = onImgDone;
                }
            }
            // Fallback only if images never finish â€” never a second print after success.
            fallbackTimer = setTimeout(triggerPrint, 1200);
        } else {
            fallbackTimer = setTimeout(triggerPrint, 250);
        }
    }

    // =========================================================================
    // 1. POS SALES RECEIPT (Cash/Card Checkout)
    // =========================================================================
    // =========================================================================
    // Format Date / Time matching thermal reference: 03/11/26' 07:46pm
    // =========================================================================
    private formatThermalDate(d: Date | string | number | undefined | null): { dateStr: string; timeStr: string; fullStr: string } {
        const dt = d ? new Date(d) : new Date();
        const valid = !isNaN(dt.getTime()) ? dt : new Date();
        const day = String(valid.getDate()).padStart(2, '0');
        const month = String(valid.getMonth() + 1).padStart(2, '0');
        const year = String(valid.getFullYear()).slice(-2);
        let hours = valid.getHours();
        const minutes = String(valid.getMinutes()).padStart(2, '0');
        const seconds = String(valid.getSeconds()).padStart(2, '0');
        const ampm = hours >= 12 ? 'pm' : 'am';
        hours = hours % 12;
        hours = hours ? hours : 12;
        const hStr = String(hours).padStart(2, '0');

        const dateStr = `${day}/${month}/${year}`;
        const timeStr = `${hStr}:${minutes}:${seconds}${ampm}`;
        const shortTimeStr = `${hStr}:${minutes}${ampm}`;
        return {
            dateStr,
            timeStr,
            fullStr: `${dateStr} ${shortTimeStr}`
        };
    }

    // =========================================================================
    // Standard Unified Reference Thermal Receipt Builder (Matches Al Meezan reference)
    // =========================================================================
    private buildReferenceReceiptHtml(params: {
        size: ThermalPaperSize;
        logoUrl?: string;
        branchName: string;
        tagline?: string;
        city?: string;
        statusLabel: string;
        tokenNo?: number | null;
        orderType: string;
        orderId: string | number;
        orderDate?: Date | string | null;
        invoiceNo?: string | number | null;
        userName?: string;
        subLocation?: string;
        lines: Array<{
            productName: string;
            quantity: number;
            unitPrice: number;
            lineTotal?: number;
            isDeal?: boolean;
            dealItems?: string[];
            modifiers?: string[];
            notes?: string;
        }>;
        subTotal: number;
        discountAmount?: number;
        discountPercent?: number;
        taxAmount?: number;
        taxPercent?: number;
        deliveryFee?: number;
        serviceChargeAmount?: number;
        serviceChargePercent?: number;
        grandTotal: number;
        cashPaid?: number;
        cardPaid?: number;
        paidAmount?: number;
        dueAmount?: number;
        changeReturned?: number;
        customerName?: string;
        customerPhone?: string;
        customerAddress?: string;
        customerLandmark?: string;
        deliveryArea?: string;
        customerPayment?: string;
        customerNote?: string;
        orderTaker?: string;
        complaintPhone?: string;
        softwareDeveloperName?: string;
        softwareDeveloperPhone?: string;
        invoiceFooter?: string;
    }): string {
        const is58 = params.size === '58mm';
        const dateParts = this.formatThermalDate(params.orderDate);
        const printParts = this.formatThermalDate(new Date());
        const branchInfo = this.getBranchInfo();

        const logoHtml = params.logoUrl
            ? `<img src="${params.logoUrl}" class="logo-img" alt="" onerror="this.style.display='none'" />`
            : '';

        const linesHtml = (params.lines || []).map((l: any) => {
            const qty = Number(l.quantity || 0);
            const rate = Number(l.unitPrice || 0);
            const total = Number(l.lineTotal != null ? l.lineTotal : qty * rate);
            const dealBadge = l.isDeal ? `<span class="badge">DEAL</span>` : '';
            const extras = [
                ...(l.dealItems || []).map((d: string) => `&bull; ${d}`),
                ...(l.modifiers?.length ? [`Opts: ${l.modifiers.join(', ')}`] : []),
                ...(l.notes ? [`* ${l.notes}`] : []),
            ];
            const extrasHtml = extras.length
                ? `<div style="font-size: ${is58 ? '8px' : '8.5px'}; margin-top: 1px; padding-left: 2px;">${extras.join('<br/>')}</div>`
                : '';

            return `
                <tr>
                    <td style="width: 48%; padding-top: 3px;">
                        <span class="bold">${dealBadge}${l.productName || 'Item'}</span>${extrasHtml}
                    </td>
                    <td style="width: 14%; text-align: center; font-weight: 700; padding-top: 3px;">${qty}</td>
                    <td style="width: 18%; text-align: right; padding-top: 3px;">${rate.toFixed(0)}</td>
                    <td style="width: 20%; text-align: right; font-weight: 700; padding-top: 3px;">${total.toFixed(0)}</td>
                </tr>
            `;
        }).join('');

        const discountHtml = (params.discountAmount && params.discountAmount > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Discount${params.discountPercent ? ' (' + params.discountPercent + '%)' : ''}</td><td style="text-align: right; padding: 1.5px 0;">-${Number(params.discountAmount).toFixed(0)} Rs</td></tr>`
            : '';
        const taxHtml = (params.taxAmount && params.taxAmount > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Tax${params.taxPercent ? ' (' + params.taxPercent + '%)' : ''}</td><td style="text-align: right; padding: 1.5px 0;">+${Number(params.taxAmount).toFixed(0)} Rs</td></tr>`
            : '';
        const deliveryHtml = (params.deliveryFee && params.deliveryFee > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Delivery Fee</td><td style="text-align: right; padding: 1.5px 0;">+${Number(params.deliveryFee).toFixed(0)} Rs</td></tr>`
            : '';
        const serviceHtml = (params.serviceChargeAmount && params.serviceChargeAmount > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Service Charge${params.serviceChargePercent ? ' (' + params.serviceChargePercent + '%)' : ''}</td><td style="text-align: right; padding: 1.5px 0;">+${Number(params.serviceChargeAmount).toFixed(0)} Rs</td></tr>`
            : '';

        const cashPaidHtml = (params.cashPaid && params.cashPaid > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Cash Paid</td><td style="text-align: right; padding: 1.5px 0;">${Number(params.cashPaid).toFixed(0)} Rs</td></tr>`
            : '';
        const cardPaidHtml = (params.cardPaid && params.cardPaid > 0)
            ? `<tr><td colspan="3" style="text-align: left; padding: 1.5px 0;">Card Paid</td><td style="text-align: right; padding: 1.5px 0;">${Number(params.cardPaid).toFixed(0)} Rs</td></tr>`
            : '';
        const dueHtml = (params.dueAmount && params.dueAmount > 0.009)
            ? `<tr style="font-weight: 700;"><td colspan="3" style="text-align: left; padding: 2px 0;">Due Amount</td><td style="text-align: right; padding: 2px 0;">${Number(params.dueAmount).toFixed(0)} Rs</td></tr>`
            : '';
        const changeHtml = (params.changeReturned && params.changeReturned > 0)
            ? `<tr style="font-weight: 700;"><td colspan="3" style="text-align: left; padding: 2px 0;">Change Returned</td><td style="text-align: right; padding: 2px 0;">${Number(params.changeReturned).toFixed(0)} Rs</td></tr>`
            : '';

        const tokenRowHtml = `
            <div style="margin-top: 4px;">
                <div class="row" style="font-size: ${is58 ? '12px' : '14px'}; font-weight: 700; margin-bottom: 2px;">
                    <span>${params.tokenNo != null && params.tokenNo > 0 ? `Token# ${params.tokenNo}` : ''}</span>
                    <span>${params.orderType}</span>
                </div>
                <div class="row" style="white-space: nowrap; font-size: ${is58 ? '8.5px' : '10.5px'};">
                    <span style="white-space: nowrap;">Order ID: <strong>${params.orderId}</strong></span>
                    <span style="white-space: nowrap;">Date: ${dateParts.fullStr}</span>
                </div>
                <div class="row" style="white-space: nowrap; font-size: ${is58 ? '8.5px' : '10.5px'};">
                    <span style="white-space: nowrap;">Invoice#: <strong>${params.invoiceNo || '#' + params.orderId}</strong></span>
                    <span style="white-space: nowrap;">User: <strong>${params.userName || 'Staff'}</strong></span>
                </div>
                <div class="row" style="white-space: nowrap; font-size: ${is58 ? '8.5px' : '10.5px'};">
                    <span style="white-space: nowrap;">Order Type : <strong>${params.orderType}</strong></span>
                    ${params.subLocation ? `<span style="white-space: nowrap;"><strong>${params.subLocation}</strong></span>` : '<span></span>'}
                </div>
            </div>
        `;

        const softwareName =
            params.softwareDeveloperName ||
            branchInfo.softwareDeveloperName ||
            'Software By AlmahirHub';
        const softwarePhone =
            params.softwareDeveloperPhone ||
            branchInfo.softwareDeveloperPhone ||
            '';

        const addressHtml = params.customerAddress
            ? `<div><span class="label">Address:</span> ${params.customerAddress}</div>`
            : '';
        const deliveryAreaHtml = params.deliveryArea
            ? `<div><span class="label">Delivery Area:</span> ${params.deliveryArea}</div>`
            : '';
        const notesHtml = params.customerNote
            ? `<div><span class="label">Notes:</span> ${params.customerNote}</div>`
            : '';
        const phoneHtml = params.customerPhone
            ? `<div><span class="label">Phone:</span> ${params.customerPhone}</div>`
            : '';
        const paymentHtml = params.customerPayment
            ? `<div><span class="label">Payment:</span> ${params.customerPayment}</div>`
            : '';

        const headerHtml = params.logoUrl
            ? `
                <div class="text-center">
                    ${logoHtml}
                    <div class="meta-text" style="margin-top: 3px; font-size: ${is58 ? '10px' : '12px'};">${params.city || 'Faisalabad'}</div>
                    <div class="bold" style="font-size: ${is58 ? '11px' : '13px'}; margin-top: 2px;">${params.statusLabel}</div>
                </div>
              `
            : `
                <div class="text-center">
                    <div class="header-title uppercase">${params.branchName}</div>
                    <div class="meta-text" style="font-style: italic; margin-top: 1px;">${params.tagline || 'Taste & Quality'}</div>
                    <div class="meta-text" style="margin-top: 2px; font-size: ${is58 ? '10px' : '12px'};">${params.city || 'Faisalabad'}</div>
                    <div class="bold" style="font-size: ${is58 ? '11px' : '13px'}; margin-top: 2px;">${params.statusLabel}</div>
                </div>
              `;

        return `
            ${headerHtml}

            ${tokenRowHtml}

            <div class="divider"></div>
            <div class="section-title">Order Detail</div>
            <div class="divider"></div>

            <table>
                <thead>
                    <tr>
                        <th style="width: 48%; text-align: left;">Item</th>
                        <th style="width: 14%; text-align: center;">Qty</th>
                        <th style="width: 18%; text-align: right;">Rate</th>
                        <th style="width: 20%; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                    <tr style="border-top: 1px dashed #000000;">
                        <td colspan="3" style="text-align: left; padding: 2.5px 0;">Sub Total</td>
                        <td style="text-align: right; font-weight: 700; padding: 2.5px 0;">${Number(params.subTotal || 0).toFixed(0)} Rs</td>
                    </tr>
                    ${discountHtml}
                    ${taxHtml}
                    ${serviceHtml}
                    ${deliveryHtml}
                    <tr style="border-top: 1px dashed #000000; border-bottom: 1px dashed #000000;">
                        <td colspan="3" style="text-align: left; font-weight: 700; font-size: ${is58 ? '11px' : '13px'}; padding: 3px 0;">GRAND TOTAL</td>
                        <td style="text-align: right; font-weight: 700; font-size: ${is58 ? '11px' : '13px'}; padding: 3px 0;">${Number(params.grandTotal || 0).toFixed(0)} Rs</td>
                    </tr>
                    ${cashPaidHtml}
                    ${cardPaidHtml}
                    ${dueHtml}
                    ${changeHtml}
                </tbody>
            </table>

            <div class="divider"></div>
            <div class="section-title">Customer Detail</div>
            <div class="divider"></div>

            <div class="customer-block">
                <div class="bold" style="font-size: ${is58 ? '11.5px' : '13px'};">${params.customerName || 'Walk-in'}</div>
                ${phoneHtml}
                ${deliveryAreaHtml}
                ${addressHtml}
                ${notesHtml}
                ${paymentHtml}
                <div class="row" style="margin-top: 4px; align-items: flex-start;">
                    <span>Order-Taker: <strong>${params.orderTaker || 'Waiter'}</strong></span>
                    <span class="printed-at" style="text-align: right;">Printed: ${printParts.dateStr}<br/>${printParts.timeStr}</span>
                </div>
            </div>

            <div class="receipt-footer" style="page-break-inside: avoid !important; break-inside: avoid !important; page-break-before: avoid !important; break-before: avoid !important;">
                <div class="divider"></div>
                <div class="text-center meta-text" style="font-size: ${is58 ? '9px' : '11px'}; line-height: 1.35;">
                    <div>For any Complaint &amp; Suggestions</div>
                    <div>Please Contact us @ ${params.complaintPhone || branchInfo.phone || '03080004248'}</div>
                </div>
                <div class="divider"></div>
                <div class="text-center meta-text" style="font-size: ${is58 ? '8.5px' : '10px'}; margin-top: 3px;">
                    <div>${softwareName}</div>
                    ${softwarePhone ? `<div>${softwarePhone}</div>` : ''}
                </div>
            </div>
        `;
    }

    private escapeThermalText(value: string): string {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    private formatUanLine(phone: string): string {
        const cleaned = String(phone || '').trim();
        if (!cleaned) {
            return 'UAN';
        }
        if (/^uan\b/i.test(cleaned)) {
            return cleaned;
        }
        return `UAN ${cleaned}`;
    }

    private formatSoftwareByLine(name: string): string {
        const cleaned = String(name || '').trim();
        if (!cleaned) {
            return 'Software By AlmahirHub';
        }
        if (/^software\s+by\b/i.test(cleaned) || /^software\s+develop/i.test(cleaned)) {
            return cleaned.replace(/^software\s+develop(?:ed)?\s+by\b/i, 'Software By');
        }
        return `Software By ${cleaned}`;
    }

    // =========================================================================
    // 1. POS SALES RECEIPT (Cash/Card Checkout)
    // =========================================================================
    public printPosReceipt(receipt: any, paperSize?: ThermalPaperSize): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const tableLabel = String(receipt.tableNo || '').toLowerCase();
        const orderTypeLabel = receipt.carNumber
            ? 'Car Service'
            : tableLabel.includes('takeaway') || tableLabel.includes('take-away')
                ? 'Take-Away'
                : tableLabel.includes('delivery')
                    ? 'Online Delivery'
                    : 'Dine-In';

        const subLocation = receipt.carNumber
            ? `Car: ${receipt.carName || ''} (${receipt.carNumber})`
            : (orderTypeLabel === 'Dine-In' ? `Table: ${receipt.tableNo || 'Table'}` : '');

        const printerIdentity = this.authService.getPrinterIdentity();
        const cityParts = (branch.address || '').split(',').map((p) => p.trim()).filter(Boolean);
        const city = cityParts.length ? cityParts[cityParts.length - 1] : 'Faisalabad';

        const total = Number(receipt.grandTotal || receipt.totalAmount || 0);
        const cashPaid = Number(receipt.cashAmount != null ? receipt.cashAmount : (receipt.paidAmount || 0));
        const cardPaid = Number(receipt.cardAmount || 0);
        const paid = cashPaid + cardPaid;
        const computedDue = Math.max(0, Math.round((total - paid) * 100) / 100);
        const dueAmount =
            receipt.dueAmount != null && receipt.dueAmount !== undefined
                ? Math.max(0, Number(receipt.dueAmount))
                : computedDue;
        const statusLabel =
            receipt.statusLabel || (dueAmount > 0.009 ? 'Unpaid' : 'Paid');
        const deliveryAreaName = (
            receipt.deliveryAreaName ||
            receipt.deliveryArea ||
            ''
        ).trim();

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: (config.showLogo !== false && branch.logoUrl) ? branch.logoUrl : undefined,
            branchName: receipt.branchName || branch.name,
            tagline: 'Taste & Quality',
            city,
            statusLabel,
            tokenNo: receipt.tokenNo,
            orderType: orderTypeLabel,
            orderId: receipt.orderNo || receipt.id,
            orderDate: receipt.date || new Date(),
            invoiceNo: receipt.invoiceNo || receipt.orderNo || receipt.id,
            userName: printerIdentity.name || receipt.branchName || branch.name,
            subLocation: subLocation || (deliveryAreaName ? `Area: ${deliveryAreaName}` : ''),
            lines: receipt.lines || [],
            subTotal: Number(receipt.subTotal || 0),
            discountAmount: Number(receipt.discountAmount || 0),
            discountPercent: Number(receipt.discountPercent || 0),
            taxAmount: Number(receipt.taxAmount || 0),
            taxPercent: Number(receipt.taxPercent || 0),
            deliveryFee: Number(receipt.deliveryFee || receipt.deliveryCharges || 0),
            serviceChargeAmount: Number(receipt.serviceChargeAmount || 0),
            serviceChargePercent: Number(receipt.serviceChargePercent || 0),
            grandTotal: total,
            cashPaid,
            cardPaid,
            dueAmount,
            changeReturned: Number(receipt.changeAmount || 0),
            customerName: receipt.customerName || 'Walk-in',
            customerPhone: receipt.customerPhone,
            customerAddress: receipt.customerAddress || receipt.deliveryAddress,
            deliveryArea: deliveryAreaName || undefined,
            orderTaker: receipt.serverName || printerIdentity.name || 'Waiter',
            complaintPhone: receipt.branchPhone || branch.phone || '03080004248',
            softwareDeveloperName: branch.softwareDeveloperName,
            softwareDeveloperPhone: branch.softwareDeveloperPhone,
            invoiceFooter: receipt.branchFooter || branch.footer,
        });

        this.executePrint(bodyHtml, `Receipt-${receipt.orderNo || receipt.id}`, size);
    }

    // =========================================================================
    // 2. KOT (Kitchen Order Ticket) & BOT (Bar Order Ticket)
    // =========================================================================
    public printKotTicket(
        kot: any,
        filterType: 'all' | 'kitchen' | 'bar' = 'all',
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        let items: any[] = kot.newItems || kot.lines || [];

        const isBarItem = (name: string) => {
            const n = (name || '').toLowerCase();
            return (
                n.includes('drink') ||
                n.includes('juice') ||
                n.includes('shake') ||
                n.includes('beverage') ||
                n.includes('tea') ||
                n.includes('coffee') ||
                n.includes('soda') ||
                n.includes('water') ||
                n.includes('cocktail') ||
                n.includes('mocktail') ||
                n.includes('bar')
            );
        };

        if (filterType === 'kitchen') {
            items = items.filter((i) => !isBarItem(i.productName));
        } else if (filterType === 'bar') {
            items = items.filter((i) => isBarItem(i.productName));
        }

        const slipTitle =
            filterType === 'bar'
                ? '*** BAR ORDER TICKET (BOT) ***'
                : '*** KITCHEN ORDER TICKET (KOT) ***';

        const linesHtml = items.length
            ? items
                .map((item: any) => {
                    const notes = item.kitchenNotes
                        ? `<div style="font-size: 9px; color: #b91c1c; font-style: italic; font-weight: bold;">NOTE: ${item.kitchenNotes}</div>`
                        : '';
                    const mods = item.selectedModifiers
                        ? `<div style="font-size: 8.5px; color: #444;">Opts: ${item.selectedModifiers}</div>`
                        : '';
                    const dealList = item.dealItems?.length
                        ? `<div style="font-size: 8.5px; color: #222; margin-top: 1px; padding-left: 6px;">
                              ${item.dealItems.map((d: string) => `<div>&bull; ${d}</div>`).join('')}
                             </div>`
                        : '';
                    const qtyChange =
                        item.previousQuantity && item.previousQuantity > 0
                            ? `<span style="font-size: 8.5px; color: #555;">(+${item.quantity} new, total: ${item.totalQuantity || item.previousQuantity + item.quantity})</span>`
                            : '';

                    return `
                        <tr style="border-bottom: 1px dashed #000;">
                            <td style="width: 22%; font-size: ${is58 ? '14px' : '18px'}; font-weight: bold; vertical-align: top; padding: 4px 0;">
                                ${item.quantity}x
                            </td>
                            <td style="width: 78%; font-size: ${is58 ? '12px' : '15px'}; font-weight: bold; padding: 4px 0;">
                                <div>${item.productName}</div>
                                ${qtyChange}
                                ${dealList}
                                ${mods}
                                ${notes}
                            </td>
                        </tr>
                      `;
                })
                .join('')
            : `<tr><td colspan="2" class="text-center meta-text" style="padding: 8px 0;">No items for this station</td></tr>`;

        const locationMeta = kot.carNumber
            ? `Car: <strong>${kot.carName || ''} (${kot.carNumber})</strong> ${kot.carLocation ? '&bull; Spot: ' + kot.carLocation : ''}`
            : `Table: <strong>${kot.tableNo || 'Takeaway'}</strong>`;

        const tokenHtml = (kot.tokenNo != null && kot.tokenNo > 0)
            ? `<div style="display: flex; justify-content: space-between; align-items: baseline; margin: 4px 0 2px 0; font-weight: bold;">
                <span style="font-size: ${is58 ? '15px' : '18px'}; font-family: monospace;">Token# ${kot.tokenNo}</span>
                <span style="font-size: ${is58 ? '13px' : '15px'};">${kot.orderTypeLabel || 'KOT'}</span>
               </div>`
            : '';

        const logoHtml = (config.showLogo !== false && branch.logoUrl)
            ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" style="margin-bottom: 3px;" />`
            : '';

        const branchTitleHtml = `<div class="header-title uppercase" style="font-size: ${is58 ? '15px' : '18px'}; font-weight: 700; letter-spacing: 0.3px; margin-top: 2px;">${branch.name}</div>`;

        const headerHtml = logoHtml
            ? `
                <div class="text-center">
                    ${logoHtml}
                    <div class="divider"></div>
                    <div class="header-subtitle uppercase" style="font-size: ${is58 ? '12px' : '14px'}; font-weight: 700; letter-spacing: 0.3px;">${slipTitle}</div>
                </div>
              `
            : `
                <div class="text-center">
                    ${branchTitleHtml}
                    <div class="divider"></div>
                    <div class="header-subtitle uppercase" style="font-size: ${is58 ? '12px' : '14px'}; font-weight: 700; letter-spacing: 0.3px;">${slipTitle}</div>
                </div>
              `;

        const bodyHtml = `
            ${headerHtml}

            ${tokenHtml}

            <div class="meta-text" style="font-size: ${is58 ? '11px' : '12.5px'}; margin: 3px 0;">
                ${locationMeta} &bull; Order: <strong>#${kot.orderNo || kot.orderId}</strong>
            </div>
            <div class="meta-text" style="font-size: ${is58 ? '9px' : '11px'};">Server: <strong>${kot.waiterName || 'Staff'}</strong> &bull; ${kot.printTime ? new Date(kot.printTime).toLocaleTimeString() : new Date().toLocaleTimeString()}</div>
            ${kot.printedByName ? `<div class="meta-text" style="font-size: ${is58 ? '9px' : '11px'};">Sent By: <strong>${kot.printedByName}</strong> (${kot.printedByRole || 'Staff'})</div>` : ''}
            <div class="divider"></div>

            <table>
                <thead>
                    <tr>
                        <th style="width: 22%; text-align: left; font-size: ${is58 ? '11px' : '13px'};">Qty</th>
                        <th style="width: 78%; text-align: left; font-size: ${is58 ? '11px' : '13px'};">Item Description</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="receipt-footer" style="page-break-inside: avoid !important; break-inside: avoid !important; page-break-before: avoid !important; break-before: avoid !important;">
                <div class="divider"></div>
                <div class="text-center meta-text" style="font-size: ${is58 ? '9px' : '11px'};">
                    <div>Print Time: ${new Date().toLocaleString()}</div>
                    <div class="bold" style="margin-top: 3px; font-size: ${is58 ? '11px' : '12.5px'};">* * * END OF ORDER * * *</div>
                    <div style="font-size: 9px; margin-top: 4px; color: #444; line-height: 1.35;">
                        <div>${branch.softwareDeveloperName || 'Software develop by AlNawabTech'}</div>
                        ${branch.softwareDeveloperPhone ? `<div>${branch.softwareDeveloperPhone}</div>` : ''}
                    </div>
                </div>
            </div>
        `;

        this.executePrint(bodyHtml, `KOT-${kot.orderNo || kot.orderId}`, size);
    }

    /**
     * Compact token sticker for kitchen â€” attach Token# to the physical order.
     */
    public printTokenSlip(order: {
        tokenNo?: number | null;
        orderNo?: string;
        tableNo?: string;
        orderTypeName?: string;
        orderTypeLabel?: string;
        carName?: string;
        carNumber?: string;
        waiterName?: string;
        customerName?: string;
    }, paperSize?: ThermalPaperSize): void {
        const token = order?.tokenNo;
        if (token == null || Number(token) <= 0) {
            throw new Error('No token number available for this order.');
        }

        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';
        const typeLabel = order.orderTypeLabel || order.orderTypeName || '';
        const location = order.carNumber
            ? `Car ${order.carName || ''} ${order.carNumber}`.trim()
            : (order.tableNo ? `Table ${order.tableNo}` : typeLabel || 'Order');

        const bodyHtml = `
            <div class="text-center">
                <div class="header-title" style="font-size: ${is58 ? '12px' : '14px'};">${branch.name || 'Kitchen'}</div>
                <div class="double-divider"></div>
                <div class="header-subtitle" style="font-size: ${is58 ? '11px' : '12px'}; letter-spacing: 0.04em;">TOKEN</div>
                <div style="font-size: ${is58 ? '42px' : '52px'}; font-weight: 900; line-height: 1.05; margin: 8px 0 6px; font-family: monospace;">
                    #${token}
                </div>
                <div class="double-divider"></div>
                <div class="meta-text bold" style="font-size: ${is58 ? '11px' : '12px'}; margin-top: 4px;">
                    ${location}
                </div>
                ${order.orderNo ? `<div class="meta-text" style="font-size: ${is58 ? '9px' : '10px'};">Order: ${order.orderNo}</div>` : ''}
                ${order.waiterName ? `<div class="meta-text" style="font-size: ${is58 ? '9px' : '10px'};">Server: ${order.waiterName}</div>` : ''}
                ${order.customerName ? `<div class="meta-text" style="font-size: ${is58 ? '9px' : '10px'};">Customer: ${order.customerName}</div>` : ''}
                <div class="meta-text" style="margin-top: 6px; font-size: ${is58 ? '8px' : '9px'};">
                    ${new Date().toLocaleString()}
                </div>
                <div class="bold" style="margin-top: 6px; font-size: ${is58 ? '9px' : '10px'};">* Attach to order *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `Token-${token}`, size);
    }

    // =========================================================================
    // 3. GUEST BILL (Pre-payment Check / Kacha Bill)
    // =========================================================================
    public printGuestBill(bill: any, paperSize?: ThermalPaperSize): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const isCar = !!bill.isCarService || !!bill.carNumber;
        const orderTypeLabel = isCar
            ? 'Car Service'
            : bill.orderTypeLabel || (bill.tableNo && bill.tableNo !== 'Takeaway' ? 'Dine-In' : 'Take-Away');

        const subLocation = isCar
            ? `Car: ${bill.carName || ''} (${bill.carNumber || ''})`
            : (orderTypeLabel === 'Dine-In' ? `Table: ${bill.tableNo || 'Table'}` : '');

        const cityParts = (branch.address || '').split(',').map((p) => p.trim()).filter(Boolean);
        const city = cityParts.length ? cityParts[cityParts.length - 1] : 'Faisalabad';

        const guestTotal = Number(bill.grandTotal || 0);
        const deliveryAreaName = (
            bill.deliveryAreaName ||
            bill.deliveryArea ||
            ''
        ).trim();

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: (config.showLogo !== false && branch.logoUrl) ? branch.logoUrl : undefined,
            branchName: branch.name,
            tagline: 'Taste & Quality',
            city,
            statusLabel: 'Unpaid',
            tokenNo: bill.tokenNo,
            orderType: orderTypeLabel,
            orderId: bill.orderNo || 'N/A',
            orderDate: new Date(),
            invoiceNo: bill.orderNo || 'N/A',
            userName: bill.waiterName || 'Staff',
            subLocation: subLocation || (deliveryAreaName ? `Area: ${deliveryAreaName}` : ''),
            lines: bill.lines || [],
            subTotal: Number(bill.subTotal || 0),
            discountAmount: Number(bill.discountAmount || 0),
            discountPercent: Number(bill.discountPercent || 0),
            taxAmount: Number(bill.taxAmount || 0),
            taxPercent: Number(bill.taxPercent || 0),
            deliveryFee: Number(bill.deliveryFee || bill.deliveryCharges || 0),
            serviceChargeAmount: Number(bill.serviceChargeAmount || 0),
            serviceChargePercent: Number(bill.serviceChargePercent || 0),
            grandTotal: guestTotal,
            dueAmount: guestTotal,
            customerName: bill.customerName || 'Walk-in',
            customerPhone: bill.customerPhone,
            customerAddress: bill.customerAddress || bill.deliveryAddress,
            deliveryArea: deliveryAreaName || undefined,
            orderTaker: bill.waiterName || 'Waiter',
            complaintPhone: branch.phone || '03080004248',
            softwareDeveloperName: branch.softwareDeveloperName,
            softwareDeveloperPhone: branch.softwareDeveloperPhone,
            invoiceFooter: branch.footer,
        });

        this.executePrint(bodyHtml, `GuestCheck-${bill.orderNo || 'Table'}`, size);
    }

    // =========================================================================
    // 4. ONLINE DELIVERY ORDER RECEIPT (Customer & Rider Details)
    // =========================================================================
    public printCustomerOrder(
        order: CustomerOrderDto,
        invoiceNo?: string,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const isDelivered = order.status === 4; // CustomerOrderStatus.Delivered
        const statusLabel = isDelivered ? 'Paid' : (order.dueAmount && order.dueAmount > 0 ? 'Unpaid' : 'Unpaid');

        const cityParts = (branch.address || '').split(',').map((p) => p.trim()).filter(Boolean);
        const city = cityParts.length ? cityParts[cityParts.length - 1] : 'Faisalabad';

        const lines = (order.lines || []).map((l: any) => ({
            productName: l.productName || `Item #${l.productId}`,
            quantity: l.quantity,
            unitPrice: l.unitPrice,
            lineTotal: l.lineTotal || (l.quantity * l.unitPrice),
            isDeal: l.isDeal,
            dealItems: l.dealItems || [],
            notes: l.notes
        }));

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: (config.showLogo !== false && branch.logoUrl) ? branch.logoUrl : undefined,
            branchName: order.branchName || branch.name,
            tagline: 'Taste & Quality',
            city,
            statusLabel,
            tokenNo: order.tokenNo,
            orderType: 'Online Delivery',
            orderId: order.orderNo || '#' + order.id,
            orderDate: order.orderDate,
            invoiceNo: invoiceNo || order.saleInvoiceNo || order.orderNo || '#' + order.id,
            userName: order.branchName || branch.name,
            subLocation: order.deliveryAreaName ? `Area: ${order.deliveryAreaName}` : '',
            lines,
            subTotal: Number(order.subTotal || 0),
            deliveryFee: Number(order.deliveryFee || 0),
            grandTotal: Number(order.totalAmount || 0),
            paidAmount: isDelivered ? Number(order.totalAmount || 0) : Number(order.paidAmount || 0),
            dueAmount: isDelivered ? 0 : Number(order.dueAmount != null ? order.dueAmount : order.totalAmount),
            customerName: order.customerName || 'Online Customer',
            customerPhone: order.customerPhone,
            deliveryArea: (order.deliveryAreaName || '').trim() || undefined,
            ...this.buildCustomerDetailForInvoice(order.customerAddress, order.notes),
            orderTaker: order.riderName
                ? String(order.riderName).replace(/^Rider:\s*/i, '').trim() || 'Rider'
                : 'Online Ordering',
            complaintPhone: branch.phone || '03080004248',
            softwareDeveloperName: branch.softwareDeveloperName,
            softwareDeveloperPhone: branch.softwareDeveloperPhone,
            invoiceFooter: branch.footer,
        });

        this.executePrint(bodyHtml, `Order-${order.orderNo || order.id}`, size);
    }

    /**
     * Online orders store delivery fields inside Notes (Addr/Landmark/Payment).
     * Never dump the raw notes blob into Address â€” that duplicates text and shows "??".
     */
    private buildCustomerDetailForInvoice(
        customerAddress?: string | null,
        notes?: string | null
    ): {
        customerAddress?: string;
        customerLandmark?: string;
        customerPayment?: string;
        customerNote?: string;
    } {
        const parsed = this.parseDeliveryFieldsFromNotes(notes);
        const rawAddr = (customerAddress || '').trim();
        const addressLooksLikeBlob =
            /Addr:|Landmark:|Maps:|Options:|Payment:|Rider\s+.+\s+delivery note|google\.com\/maps|maps\.google|\?\?/i.test(
                rawAddr
            );

        let address = this.cleanInvoiceText(
            parsed.address || (!addressLooksLikeBlob ? rawAddr : '')
        );
        if (!address && parsed.address) {
            address = this.cleanInvoiceText(parsed.address);
        }

        // Notes line = customer note + landmark + rider delivery note (never payment/address).
        const noteParts = [
            this.cleanInvoiceText(parsed.specialNote),
            this.cleanInvoiceText(parsed.landmark),
            this.cleanInvoiceText(parsed.riderNote),
        ].filter((p) => !!p && (!address || p.toLowerCase() !== address.toLowerCase()));

        return {
            customerAddress: address || undefined,
            customerPayment: this.cleanInvoiceText(parsed.payment) || undefined,
            customerNote: noteParts.length ? noteParts.join(' Â· ') : undefined,
        };
    }

    private cleanInvoiceText(value?: string | null): string {
        if (!value) {
            return '';
        }
        return String(value)
            .replace(/[ðŸ“ðŸ›ï¸ðŸ—ºï¸ðŸ“ðŸ’³ðŸ“?\uFFFD]+/gu, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    private parseDeliveryFieldsFromNotes(notes?: string | null): {
        address?: string;
        landmark?: string;
        payment?: string;
        specialNote?: string;
        riderNote?: string;
    } {
        let text = (notes || '').trim();
        if (!text) {
            return {};
        }

        const clean = (value?: string | null) => {
            const v = this.cleanInvoiceText(
                (value || '')
                    .replace(/^(?:Addr|Landmark|Maps|Notes|Payment|Distance)\s*:\s*/i, '')
            );
            return v || undefined;
        };

        // Pull rider delivery notes out first so they never stick to Payment/Address.
        let riderNote: string | undefined;
        const riderMatch =
            text.match(/\[Rider[^\]]*delivery note:\s*([^\]]+)\]/i) ||
            text.match(/Rider\s+[^\]]*?delivery note:\s*([^\]]+)/i);
        if (riderMatch?.[1]) {
            riderNote = clean(riderMatch[1]);
            text = text
                .replace(/\[Rider[^\]]*delivery note:\s*[^\]]+\]/gi, ' ')
                .replace(/Rider\s+[^\]]*?delivery note:\s*[^\]]+/gi, ' ')
                .replace(/\s+/g, ' ')
                .trim();
        }

        const addrMatch = text.match(/(?:ðŸ“\s*)?Addr:\s*([^|]+)/i);
        const landmarkMatch = text.match(/(?:ðŸ›ï¸\s*)?Landmark:\s*([^|]+)/i);
        const paymentMatch = text.match(/(?:ðŸ’³\s*)?Payment:\s*([^|]+)/i);
        const notesMatch = text.match(/(?:ðŸ“\s*)?Notes:\s*([^|]+)/i);

        let address = clean(addrMatch?.[1]);
        let landmark = clean(landmarkMatch?.[1]);
        let payment = clean(paymentMatch?.[1]);
        let specialNote = clean(notesMatch?.[1]);

        // Payment must never include rider/delivery note fragments.
        if (payment) {
            payment = clean(
                payment
                    .replace(/\[Rider[^\]]*\]/gi, ' ')
                    .replace(/Rider\s+.+$/i, ' ')
            );
        }

        // Legacy blobs used "?? address" when emoji encoding failed.
        if (!address) {
            const legacyAddr = text.match(/(?:^|\|)\s*\?{1,3}\s*([^|]+)/);
            const candidate = clean(legacyAddr?.[1]);
            if (
                candidate &&
                !/^(payment|distance|maps|landmark|options)\b/i.test(candidate) &&
                !/maps\.google|google\.com\/maps/i.test(candidate)
            ) {
                address = candidate;
            }
        }

        // Drop structured/noisy leftovers; keep only a real customer note.
        const leftovers = text
            .replace(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/https?:\/\/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:ðŸ“\s*)?Addr:\s*[^|]+/gi, '')
            .replace(/(?:ðŸ›ï¸\s*)?Landmark:\s*[^|]+/gi, '')
            .replace(/(?:ðŸ—ºï¸\s*)?Maps:\s*[^|]+/gi, '')
            .replace(/(?:ðŸ“\s*)?Notes:\s*[^|]+/gi, '')
            .replace(/(?:ðŸ’³\s*)?Payment:\s*[^|]*/gi, '')
            .replace(/(?:ðŸ“\s*)?Distance:\s*[^|]*/gi, '')
            .replace(/Options:\s*[^|]*/gi, '')
            .replace(/\[Rider[^\]]*\]/gi, '')
            .replace(/\|\s*/g, ' ')
            .replace(/[?ï¼Ÿ\uFFFD]+/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();

        if (leftovers) {
            const lower = leftovers.toLowerCase();
            if (
                !address &&
                leftovers.length > 8 &&
                !lower.startsWith('payment') &&
                !/\(\+?pkr/i.test(leftovers) &&
                !/\d+x\s/i.test(leftovers)
            ) {
                address = leftovers;
            } else if (
                !specialNote &&
                (!address || lower !== address.toLowerCase()) &&
                leftovers.length > 2 &&
                !lower.startsWith('payment')
            ) {
                specialNote = leftovers;
            }
        }

        if (landmark && address && landmark.toLowerCase() === address.toLowerCase()) {
            landmark = undefined;
        }
        if (specialNote && address && specialNote.toLowerCase() === address.toLowerCase()) {
            specialNote = undefined;
        }

        return { address, landmark, payment, specialNote, riderNote };
    }

    // =========================================================================
    // 5. SALE INVOICE (Sales List)
    // =========================================================================
    public printSaleInvoice(
        sale: SaleDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const due = Number(sale.dueAmount ?? 0);
        const payStatus = due > 0.009 ? 'Unpaid' : (sale.paymentType === 2 ? 'Credit' : 'Paid');
        const notesLower = (sale.notes || '').toLowerCase();
        let orderType = sale.saleType || 'Take-Away';
        if (notesLower.includes('takeaway') || notesLower.includes('take-away') || notesLower.includes('take away')) {
            orderType = 'Take-Away';
        } else if (notesLower.includes('dine-in') || notesLower.includes('dine in') || notesLower.includes('table')) {
            orderType = 'Dine-In';
        } else if (notesLower.includes('car service') || notesLower.includes('car:')) {
            orderType = 'Car Service';
        } else if (notesLower.includes('online order') || notesLower.includes('delivery')) {
            orderType = 'Online Delivery';
        }

        const cityParts = (branch.address || '').split(',').map((p) => p.trim()).filter(Boolean);
        const city = cityParts.length ? cityParts[cityParts.length - 1] : 'Faisalabad';

        const lines = (sale.lines || []).map((l: any) => ({
            productName: l.productName || `Item #${l.productId}`,
            quantity: l.quantity,
            unitPrice: l.unitPrice,
            lineTotal: l.lineTotal || (l.quantity * l.unitPrice),
            isDeal: l.isDeal,
            dealItems: l.dealItems || []
        }));

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: (config.showLogo !== false && branch.logoUrl) ? branch.logoUrl : undefined,
            branchName: overrideCompany?.name || branch.name,
            tagline: 'Taste & Quality',
            city,
            statusLabel: payStatus,
            tokenNo: sale.tokenNo,
            orderType,
            orderId: sale.id,
            orderDate: sale.saleDate,
            invoiceNo: sale.invoiceNo || '#' + sale.id,
            userName: this.authService.getPrinterIdentity().name || overrideCompany?.name || branch.name,
            subLocation: sale.deliveryAreaName ? `Area: ${sale.deliveryAreaName}` : '',
            lines,
            subTotal: Number(sale.subTotal || 0),
            discountAmount: Number(sale.discountAmount || 0),
            discountPercent: Number(sale.discountPercent || 0),
            taxAmount: Number(sale.taxAmount || 0),
            taxPercent: Number(sale.taxPercent || 0),
            deliveryFee: Number(sale.deliveryCharges || 0),
            grandTotal: Number(sale.totalAmount || 0),
            cashPaid: Number(sale.cashAmount || 0),
            cardPaid: Number(sale.cardAmount || 0),
            dueAmount: due,
            customerName: sale.customerName || 'Walk-in',
            customerPhone: (sale as any).customerPhone,
            deliveryArea: (sale.deliveryAreaName || '').trim() || undefined,
            orderTaker: this.authService.getPrinterIdentity().name || 'Staff',
            complaintPhone: branch.phone || '03080004248',
            softwareDeveloperName: branch.softwareDeveloperName || overrideCompany?.softwareDeveloperName,
            softwareDeveloperPhone: branch.softwareDeveloperPhone || overrideCompany?.softwareDeveloperPhone,
            invoiceFooter: branch.footer || overrideCompany?.invoiceFooter,
        });

        this.executePrint(bodyHtml, `Sale-${sale.invoiceNo || sale.id}`, size);
    }
    public printSaleReturn(
        saleReturn: SaleReturnDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (saleReturn.lines || [])
            .map(
                (l) => `
                <tr style="border-bottom: 1px dotted #ccc;">
                    <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${l.quantity}x</td>
                    <td style="width: ${is58 ? '48%' : '50%'};">${l.productName || 'Item #' + l.productId}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(l.unitPrice).toFixed(2)}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(l.lineTotal).toFixed(2)}</td>
                </tr>
            `
            )
            .join('');

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** SALE RETURN RECEIPT ***</div>
                <div class="meta-text bold">Return #: #${saleReturn.id}</div>
                <div class="meta-text">Original Invoice: ${saleReturn.saleInvoiceNo || 'N/A'}</div>
                <div class="meta-text">Date: ${new Date(saleReturn.returnDate).toLocaleString()}</div>
                <div class="meta-text">Customer: ${saleReturn.customerName || 'Walk-in'}</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '16%' : '14%'}; text-align: center;">Qty</th>
                        <th style="width: ${is58 ? '48%' : '50%'}; text-align: left;">Returned Item</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Rate</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row grand-total">
                <span>TOTAL REFUND:</span>
                <span>${Number(saleReturn.totalAmount || 0).toFixed(2)}</span>
            </div>

            ${saleReturn.notes ? `<div class="meta-text" style="font-style:italic; margin: 3px 0;">Reason/Notes: ${saleReturn.notes}</div>` : ''}

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>Customer Signature: __________________</div>
                <div style="margin-top: 4px; font-size: 8px;">* * * RETURN PROCESSED * * *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `SaleReturn-#${saleReturn.id}`, size);
    }

    // =========================================================================
    // 7. PURCHASE INVOICE / VENDOR BILL
    // =========================================================================
    public printPurchaseInvoice(
        purchase: PurchaseDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (purchase.lines || [])
            .map(
                (l) => `
                <tr style="border-bottom: 1px dotted #ccc;">
                    <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${l.quantity}x</td>
                    <td style="width: ${is58 ? '48%' : '50%'};">${l.productName || 'Item #' + l.productId}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(l.unitCost).toFixed(2)}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(l.lineTotal).toFixed(2)}</td>
                </tr>
            `
            )
            .join('');

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** PURCHASE INVOICE ***</div>
                <div class="meta-text bold">Purchase #: #${purchase.id}</div>
                ${purchase.invoiceNo ? `<div class="meta-text">Supplier Bill #: ${purchase.invoiceNo}</div>` : ''}
                <div class="meta-text">Date: ${new Date(purchase.purchaseDate).toLocaleString()}</div>
                <div class="meta-text">Supplier: ${purchase.supplierName || 'N/A'}</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '16%' : '14%'}; text-align: center;">Qty</th>
                        <th style="width: ${is58 ? '48%' : '50%'}; text-align: left;">Item</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Cost</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row grand-total">
                <span>TOTAL AMOUNT:</span>
                <span>${Number(purchase.totalAmount || 0).toFixed(2)}</span>
            </div>

            <div class="row"><span>Amount Paid:</span><span>${Number(purchase.amountPaid || 0).toFixed(2)}</span></div>
            ${purchase.dueAmount && purchase.dueAmount > 0 ? `<div class="row bold"><span>Balance Due:</span><span>${Number(purchase.dueAmount).toFixed(2)}</span></div>` : ''}

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>Authorized By: __________________</div>
                <div style="margin-top: 4px; font-size: 8px;">* * * END OF PURCHASE RECEIPT * * *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `Purchase-#${purchase.id}`, size);
    }

    // =========================================================================
    // 8. PURCHASE RETURN RECEIPT
    // =========================================================================
    public printPurchaseReturn(
        purchaseReturn: PurchaseReturnDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (purchaseReturn.lines || [])
            .map(
                (l) => `
                <tr style="border-bottom: 1px dotted #ccc;">
                    <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${l.quantity}x</td>
                    <td style="width: ${is58 ? '48%' : '50%'};">${l.productName || 'Item #' + l.productId}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(l.unitCost).toFixed(2)}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(l.lineTotal).toFixed(2)}</td>
                </tr>
            `
            )
            .join('');

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** DEBIT NOTE / PURCHASE RETURN ***</div>
                <div class="meta-text bold">Return #: #${purchaseReturn.id}</div>
                <div class="meta-text">Purchase Invoice: ${purchaseReturn.purchaseInvoiceNo || 'N/A'}</div>
                <div class="meta-text">Date: ${new Date(purchaseReturn.returnDate).toLocaleString()}</div>
                <div class="meta-text">Supplier: ${purchaseReturn.supplierName || 'N/A'}</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '16%' : '14%'}; text-align: center;">Qty</th>
                        <th style="width: ${is58 ? '48%' : '50%'}; text-align: left;">Returned Item</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Cost</th>
                        <th style="width: ${is58 ? '18%' : '18%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row grand-total">
                <span>TOTAL DEBIT AMOUNT:</span>
                <span>${Number(purchaseReturn.totalAmount || 0).toFixed(2)}</span>
            </div>

            ${purchaseReturn.notes ? `<div class="meta-text" style="font-style:italic;">Notes: ${purchaseReturn.notes}</div>` : ''}

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>Supplier Signature: __________________</div>
                <div style="margin-top: 4px; font-size: 8px;">* * * DEBIT NOTE COMPLETE * * *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `PurchaseReturn-#${purchaseReturn.id}`, size);
    }

    // =========================================================================
    // 9. EXPENSE VOUCHER
    // =========================================================================
    public printExpenseVoucher(
        expense: ExpenseDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** EXPENSE PAYMENT VOUCHER ***</div>
                <div class="meta-text bold">Voucher #: #${expense.id}</div>
                ${expense.referenceNo ? `<div class="meta-text">Ref / Bill #: ${expense.referenceNo}</div>` : ''}
                <div class="meta-text">Date: ${new Date(expense.expenseDate).toLocaleString()}</div>
                <div class="divider"></div>
            </div>

            <div class="row"><span>Paid From Account:</span><strong>${expense.paymentAccountName || 'Cash Account'}</strong></div>
            <div class="row"><span>Expense Category:</span><strong>${expense.expenseAccountName || 'General Expense'}</strong></div>
            <div class="row" style="margin-top: 4px;"><span>Description:</span><span>${expense.description || 'â€”'}</span></div>

            <div class="row grand-total" style="margin-top: 8px;">
                <span>VOUCHER AMOUNT:</span>
                <span>${Number(expense.amount || 0).toFixed(2)}</span>
            </div>

            ${expense.createdByName ? `<div class="row meta-text"><span>Prepared By:</span><span>${expense.createdByName}</span></div>` : ''}

            <div class="double-divider"></div>
            <div class="meta-text" style="margin-top: 10px; display: flex; justify-content: space-between;">
                <div>Receiver Signature: ____________</div>
                <div>Authorized: ____________</div>
            </div>
            <div class="text-center meta-text" style="margin-top: 8px; font-size: 8px;">* * * EXPENSE RECORDED * * *</div>
        `;

        this.executePrint(bodyHtml, `Expense-#${expense.id}`, size);
    }

    // =========================================================================
    // 10. PAYMENT / CASH IN / CASH OUT / LEDGER VOUCHER
    // =========================================================================
    public printLedgerReceipt(
        entry: LedgerEntryDto,
        overrideCompany?: BranchDto | null,
        paperSize?: ThermalPaperSize
    ): void {
        const branch = this.getBranchInfo(overrideCompany);
        const config = this.getConfig();
        const size = paperSize || config.paperSize;

        const isDebit = (entry.debit || 0) > 0;
        const amount = isDebit ? entry.debit : entry.credit;
        const transactionType = isDebit
            ? entry.voucherType || 'CASH OUT / DEBIT'
            : entry.voucherType || 'CASH IN / CREDIT';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** PAYMENT RECEIPT / VOUCHER ***</div>
                <div class="meta-text bold">Entry #: #${entry.id}</div>
                <div class="meta-text">Type: <strong>${transactionType}</strong></div>
                <div class="meta-text">Date: ${new Date(entry.transactionDate).toLocaleString()}</div>
                <div class="divider"></div>
            </div>

            <div class="row"><span>Account Name:</span><strong>${entry.accountName || 'Cash Account'}</strong></div>
            <div class="row"><span>Transaction Type:</span><span>${isDebit ? 'DEBIT (Paid Out)' : 'CREDIT (Received In)'}</span></div>
            ${entry.description ? `<div class="row" style="margin-top: 3px;"><span>Particulars:</span><span>${entry.description}</span></div>` : ''}

            <div class="row grand-total" style="margin-top: 8px;">
                <span>AMOUNT:</span>
                <span>${Number(amount || 0).toFixed(2)}</span>
            </div>

            <div class="double-divider"></div>
            <div class="meta-text" style="margin-top: 10px; display: flex; justify-content: space-between;">
                <div>Cashier: ${this.authService.getPrinterIdentity().name}</div>
                <div>Authorized Sign: __________</div>
            </div>
            <div class="text-center meta-text" style="margin-top: 8px; font-size: 8px;">* * * THANK YOU * * *</div>
        `;

        this.executePrint(bodyHtml, `Voucher-#${entry.id}`, size);
    }

    // =========================================================================
    // 11. DIAGNOSTIC TEST RECEIPT
    // =========================================================================
    public printTestReceipt(paperSize?: ThermalPaperSize): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                <div class="meta-text">${branch.address || 'SmartPOS Cloud Terminal'}</div>
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** PRINTER DIAGNOSTIC TEST ***</div>
                <div class="meta-text bold" style="margin-top: 3px;">Paper: ${size}</div>
                <div class="meta-text">Print Width: ${is58 ? '48mm (32 Chars)' : '72mm (48 Chars)'}</div>
                <div class="meta-text">Timestamp: ${new Date().toLocaleString()}</div>
                <div class="divider"></div>
            </div>

            <div class="meta-text" style="margin: 6px 0;">
                <div class="bold">ALIGNMENT & COLUMN CHECK:</div>
                <div style="display:flex; justify-content:space-between;"><span>[LEFT]</span><span>[CENTER]</span><span>[RIGHT]</span></div>
                <div style="font-family:monospace; margin-top:3px; word-break:break-all;">
                    ${is58 ? '12345678901234567890123456789012' : '123456789012345678901234567890123456789012345678'}
                </div>
            </div>

            <div class="divider"></div>

            <div class="row"><span>Status:</span><span class="bold">PASS / READY</span></div>
            <div class="row"><span>Auto-Print:</span><span>${config.autoPrint ? 'ENABLED' : 'DISABLED'}</span></div>
            <div class="row"><span>Copies:</span><span>${config.copies} Copy</span></div>

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div class="bold">THERMAL PRINTER CONFIGURATION OK</div>
                <div>${config.footerMessage}</div>
                <div style="margin-top: 3px; font-size: 8px;">* * * END OF TEST * * *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `TestReceipt-${size}`, size);
    }
}

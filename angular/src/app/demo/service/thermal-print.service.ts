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
        const bodyWidth = is58 ? '48mm' : '72mm';
        const baseFontSize = is58 ? '9.5px' : '12px';
        const titleFontSize = is58 ? '13px' : '16px';
        const subTitleFontSize = is58 ? '10.5px' : '13px';
        const metaFontSize = is58 ? '8.5px' : '10.5px';

        return `
            @page {
                margin: 0mm !important;
            }
            @media print {
                html, body {
                    width: ${bodyWidth} !important;
                    max-width: ${bodyWidth} !important;
                    margin: 0 auto !important;
                    padding: ${is58 ? '1mm' : '2mm'} !important;
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
                font-family: 'Courier New', Courier, 'Lucida Console', Monaco, monospace;
                font-size: ${baseFontSize};
                line-height: 1.25;
                color: #000000;
                background: #ffffff;
                width: ${bodyWidth};
                max-width: ${bodyWidth};
                margin: 0 auto;
                padding: ${is58 ? '2mm 1mm' : '4mm 2mm'};
            }
            .text-center { text-align: center; }
            .text-right { text-align: right; }
            .text-left { text-align: left; }
            .bold { font-weight: bold; }
            .uppercase { text-transform: uppercase; }
            .divider { border-top: 1px dashed #000000; margin: 4px 0; }
            .double-divider { border-top: 2px solid #000000; margin: 5px 0; }
            .dotted-divider { border-top: 1px dotted #000000; margin: 3px 0; }
            .row {
                display: flex;
                justify-content: space-between;
                font-size: ${metaFontSize};
                margin-bottom: 2px;
                line-height: 1.25;
            }
            .header-title {
                font-size: ${titleFontSize};
                font-weight: bold;
                letter-spacing: 0.5px;
            }
            .header-subtitle {
                font-size: ${subTitleFontSize};
                font-weight: bold;
                margin-top: 2px;
            }
            .meta-text {
                font-size: ${metaFontSize};
                color: #111111;
                line-height: 1.25;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin: 3px 0;
                table-layout: fixed;
            }
            th {
                border-bottom: 1px dashed #000000;
                padding: 3px 2px;
                font-size: ${metaFontSize};
                font-weight: bold;
                word-break: break-word;
            }
            td {
                padding: 3px 2px;
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
                max-width: ${is58 ? '36mm' : '48mm'};
                max-height: 45px;
                margin: 0 auto 3px auto;
                display: block;
                filter: grayscale(100%) contrast(160%);
                object-fit: contain;
            }
            .badge {
                display: inline-block;
                background: #000000;
                color: #ffffff;
                padding: 1px 3px;
                font-size: 8px;
                font-weight: bold;
                border-radius: 2px;
                margin-right: 2px;
            }
            .grand-total {
                font-size: ${subTitleFontSize};
                font-weight: bold;
                border-top: 1px dashed #000000;
                border-bottom: 1px dashed #000000;
                padding: 4px 0;
                margin: 4px 0;
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
                finalContent += `<div class="receipt-copy-separator">✂ - - - - - - CUT HERE - - - - - - ✂</div>`;
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

        // Guard: logo/img onload + fallback timeout previously both called print() → duplicate invoices.
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
            // Fallback only if images never finish — never a second print after success.
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

        const dateStr = `${day}/${month}/${year}'`;
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
        orderTaker?: string;
        complaintPhone?: string;
    }): string {
        const is58 = params.size === '58mm';
        const dateParts = this.formatThermalDate(params.orderDate);
        const printParts = this.formatThermalDate(new Date());

        const logoHtml = params.logoUrl
            ? `<img src="${params.logoUrl}" class="logo-img" alt="Logo" />`
            : '';

        const linesHtml = (params.lines || []).map((l: any) => {
            const qty = Number(l.quantity || 0);
            const rate = Number(l.unitPrice || 0);
            const total = Number(l.lineTotal != null ? l.lineTotal : qty * rate);
            const dealBadge = l.isDeal ? `<span class="badge">DEAL</span>` : '';
            const dealList = (l.dealItems && l.dealItems.length > 0)
                ? `<div style="font-size: ${is58 ? '8px' : '9px'}; color: #222; margin-top: 1px; padding-left: 6px;">
                    ${l.dealItems.map((d: string) => `<div>&bull; ${d}</div>`).join('')}
                   </div>`
                : '';
            const mods = l.modifiers?.length
                ? `<div style="font-size: ${is58 ? '8px' : '8.5px'}; color: #555;">Opts: ${l.modifiers.join(', ')}</div>`
                : '';
            const notes = l.notes
                ? `<div style="font-size: ${is58 ? '8px' : '8.5px'}; color: #b91c1c; font-style: italic;">* ${l.notes}</div>`
                : '';

            return `
                <tr>
                    <td colspan="4" style="padding-top: 2px; padding-bottom: 0;">
                        <span style="font-weight: bold;">${dealBadge}${l.productName || 'Item'}</span>
                        ${dealList}${mods}${notes}
                    </td>
                </tr>
                <tr style="border-bottom: 1px dotted #ccc;">
                    <td style="width: 46%; padding-top: 0;"></td>
                    <td style="width: 14%; text-align: center; font-weight: bold; padding-top: 0;">${qty}</td>
                    <td style="width: 20%; text-align: right; padding-top: 0;">${rate.toFixed(0)}</td>
                    <td style="width: 20%; text-align: right; font-weight: bold; padding-top: 0;">${total.toFixed(0)}</td>
                </tr>
            `;
        }).join('');

        const discountHtml = (params.discountAmount && params.discountAmount > 0)
            ? `<div class="row"><span>Discount${params.discountPercent ? ' (' + params.discountPercent + '%)' : ''}:</span><span>-${Number(params.discountAmount).toFixed(0)}</span></div>`
            : '';
        const taxHtml = (params.taxAmount && params.taxAmount > 0)
            ? `<div class="row"><span>Tax${params.taxPercent ? ' (' + params.taxPercent + '%)' : ''}:</span><span>+${Number(params.taxAmount).toFixed(0)}</span></div>`
            : '';
        const deliveryHtml = (params.deliveryFee && params.deliveryFee > 0)
            ? `<div class="row"><span>Delivery Charges:</span><span>+${Number(params.deliveryFee).toFixed(0)}</span></div>`
            : '';
        const serviceHtml = (params.serviceChargeAmount && params.serviceChargeAmount > 0)
            ? `<div class="row"><span>Service Charge${params.serviceChargePercent ? ' (' + params.serviceChargePercent + '%)' : ''}:</span><span>+${Number(params.serviceChargeAmount).toFixed(0)}</span></div>`
            : '';

        const cashPaidHtml = (params.cashPaid && params.cashPaid > 0)
            ? `<div class="row text-xs"><span>Cash Paid:</span><span>${Number(params.cashPaid).toFixed(0)}</span></div>`
            : '';
        const cardPaidHtml = (params.cardPaid && params.cardPaid > 0)
            ? `<div class="row text-xs"><span>Card Paid:</span><span>${Number(params.cardPaid).toFixed(0)}</span></div>`
            : '';
        const dueHtml = (params.dueAmount && params.dueAmount > 0.009)
            ? `<div class="row bold" style="color: #b91c1c;"><span>Due Amount:</span><span>${Number(params.dueAmount).toFixed(0)} Rs</span></div>`
            : '';
        const changeHtml = (params.changeReturned && params.changeReturned > 0)
            ? `<div class="row bold"><span>Change Returned:</span><span>${Number(params.changeReturned).toFixed(0)} Rs</span></div>`
            : '';

        const tokenRowHtml = (params.tokenNo != null && params.tokenNo > 0)
            ? `<div style="display: flex; justify-content: space-between; align-items: baseline; margin-top: 5px; margin-bottom: 2px; font-weight: bold;">
                <span style="font-size: ${is58 ? '13px' : '15px'}; font-family: monospace;">Token# ${params.tokenNo}</span>
                <span style="font-size: ${is58 ? '12px' : '14px'};">${params.orderType}</span>
               </div>`
            : `<div style="text-align: right; margin-top: 4px; font-weight: bold; font-size: ${is58 ? '12px' : '14px'};">${params.orderType}</div>`;

        return `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title uppercase bold" style="font-size: ${is58 ? '14px' : '16px'}; letter-spacing: 0.5px;">${params.branchName}</div>
                <div class="meta-text" style="font-size: ${is58 ? '10px' : '11px'}; margin-top: 1px;">${params.tagline || 'Taste & Quality'}</div>
                <div class="meta-text" style="font-size: ${is58 ? '9.5px' : '10.5px'}; margin-top: 1px;">${params.city || 'Faisalabad'}</div>
                <div class="meta-text bold uppercase" style="font-size: ${is58 ? '10.5px' : '11.5px'}; margin-top: 3px;">${params.statusLabel}</div>
            </div>

            ${tokenRowHtml}

            <div style="font-size: ${is58 ? '8.5px' : '10px'}; margin-top: 3px; line-height: 1.35;">
                <div class="row">
                    <span>Order ID: <strong>${params.orderId}</strong></span>
                    <span>Date: ${dateParts.fullStr}</span>
                </div>
                <div class="row">
                    <span>Invoice#: <strong>${params.invoiceNo || '#' + params.orderId}</strong></span>
                    <span>User: <strong>${params.userName || 'Staff'}</strong></span>
                </div>
                <div class="row">
                    <span>Order Type : <strong>${params.orderType}</strong></span>
                    ${params.subLocation ? `<span><strong>${params.subLocation}</strong></span>` : ''}
                </div>
            </div>

            <div class="divider"></div>
            <div class="text-center bold uppercase" style="font-size: ${is58 ? '10.5px' : '12px'}; margin: 2px 0;">Order Detail</div>
            <div class="divider" style="margin: 2px 0 4px 0;"></div>

            <table style="width: 100%; border-collapse: collapse; table-layout: fixed;">
                <thead>
                    <tr style="border-bottom: 1px dashed #000; font-size: ${is58 ? '9px' : '10.5px'};">
                        <th style="width: 46%; text-align: left; padding: 2px 0;">Item</th>
                        <th style="width: 14%; text-align: center; padding: 2px 0;">Qty</th>
                        <th style="width: 20%; text-align: right; padding: 2px 0;">Rate</th>
                        <th style="width: 20%; text-align: right; padding: 2px 0;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div style="font-size: ${is58 ? '9.5px' : '10.5px'}; line-height: 1.35;">
                <div class="row"><span>Sub Total</span><span class="bold">${Number(params.subTotal || 0).toFixed(0)} Rs</span></div>
                ${discountHtml}
                ${taxHtml}
                ${serviceHtml}
                ${deliveryHtml}

                <div class="row grand-total bold" style="font-size: ${is58 ? '11px' : '13px'};">
                    <span>GRAND TOTAL</span>
                    <span>${Number(params.grandTotal || 0).toFixed(0)} Rs</span>
                </div>

                ${cashPaidHtml}
                ${cardPaidHtml}
                ${dueHtml}
                ${changeHtml}
            </div>

            <div class="divider"></div>
            <div class="text-center bold uppercase" style="font-size: ${is58 ? '10.5px' : '12px'}; margin: 2px 0;">Customer Detail</div>
            <div class="divider" style="margin: 2px 0 4px 0;"></div>

            <div style="font-size: ${is58 ? '9px' : '10.5px'}; line-height: 1.35;">
                <div class="bold">${params.customerName || 'Walk-in'}</div>
                ${params.customerPhone ? `<div>Phone: ${params.customerPhone}</div>` : ''}
                ${params.customerAddress ? `<div>Address: ${params.customerAddress}</div>` : ''}
                <div class="row" style="margin-top: 4px;">
                    <span>Order-Taker: <strong>${params.orderTaker || 'Waiter'}</strong></span>
                    <span style="text-align: right;">Printed: ${printParts.dateStr}<br>${printParts.timeStr}</span>
                </div>
            </div>

            <div class="divider"></div>
            <div class="text-center meta-text" style="font-size: ${is58 ? '8.5px' : '10px'}; line-height: 1.3;">
                <div>For any Complaint &amp; Suggestions</div>
                <div>Please Contact us @ ${params.complaintPhone || '03080004248'}</div>
            </div>
            <div class="divider"></div>
            <div class="text-center meta-text" style="font-size: 8.5px; margin-top: 2px; line-height: 1.35;">
                <div>Software develop by AlNawabTech</div>
                <div>03167854606</div>
            </div>
        `;
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

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: config.showLogo && branch.logoUrl ? branch.logoUrl : undefined,
            branchName: receipt.branchName || branch.name,
            tagline: 'Taste & Quality',
            city,
            statusLabel: 'Paid',
            tokenNo: receipt.tokenNo,
            orderType: orderTypeLabel,
            orderId: receipt.orderNo || receipt.id,
            orderDate: receipt.date || new Date(),
            invoiceNo: receipt.invoiceNo || receipt.orderNo || receipt.id,
            userName: printerIdentity.name || receipt.branchName || branch.name,
            subLocation,
            lines: receipt.lines || [],
            subTotal: Number(receipt.subTotal || 0),
            discountAmount: Number(receipt.discountAmount || 0),
            discountPercent: Number(receipt.discountPercent || 0),
            taxAmount: Number(receipt.taxAmount || 0),
            taxPercent: Number(receipt.taxPercent || 0),
            deliveryFee: Number(receipt.deliveryFee || 0),
            serviceChargeAmount: Number(receipt.serviceChargeAmount || 0),
            serviceChargePercent: Number(receipt.serviceChargePercent || 0),
            grandTotal: Number(receipt.grandTotal || receipt.totalAmount || 0),
            cashPaid: Number(receipt.paidAmount || receipt.cashAmount || 0),
            cardPaid: Number(receipt.cardAmount || 0),
            changeReturned: Number(receipt.changeAmount || 0),
            customerName: receipt.customerName || 'Walk-in',
            orderTaker: receipt.serverName || printerIdentity.name || 'Waiter',
            complaintPhone: receipt.branchPhone || branch.phone || '03080004248',
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
                            <td style="width: 25%; font-size: ${is58 ? '12px' : '14px'}; font-weight: bold; vertical-align: top;">
                                ${item.quantity}x
                            </td>
                            <td style="width: 75%; font-size: ${is58 ? '10px' : '12px'}; font-weight: bold;">
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
                <span style="font-size: ${is58 ? '13px' : '15px'}; font-family: monospace;">Token# ${kot.tokenNo}</span>
                <span style="font-size: ${is58 ? '12px' : '14px'};">${kot.orderTypeLabel || 'KOT'}</span>
               </div>`
            : '';

        const bodyHtml = `
            <div class="text-center">
                <div class="header-title">${branch.name}</div>
                <div class="double-divider"></div>
                <div class="header-subtitle">${slipTitle}</div>
            </div>

            ${tokenHtml}

            <div class="meta-text bold" style="font-size: ${is58 ? '11px' : '13px'}; margin: 3px 0;">
                ${locationMeta} &bull; Order: #${kot.orderNo || kot.orderId}
            </div>
            <div class="meta-text">Server: ${kot.waiterName || 'Staff'} &bull; ${kot.printTime ? new Date(kot.printTime).toLocaleTimeString() : new Date().toLocaleTimeString()}</div>
            ${kot.printedByName ? `<div class="meta-text">Sent By: ${kot.printedByName} (${kot.printedByRole || 'Staff'})</div>` : ''}
            <div class="divider"></div>

            <table>
                <thead>
                    <tr>
                        <th style="width: 25%; text-align: left;">Qty</th>
                        <th style="width: 75%; text-align: left;">Item Description</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="double-divider"></div>
            <div class="text-center meta-text">
                <div>Print Time: ${new Date().toLocaleString()}</div>
                <div class="bold" style="margin-top: 3px;">* * * END OF ORDER * * *</div>
                <div style="font-size: 8.5px; margin-top: 4px; color: #444; line-height: 1.35;">
                    <div>Software develop by AlNawabTech</div>
                    <div>03167854606</div>
                </div>
            </div>
        `;

        this.executePrint(bodyHtml, `KOT-${kot.orderNo || kot.orderId}`, size);
    }

    /**
     * Compact token sticker for kitchen — attach Token# to the physical order.
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

        const bodyHtml = this.buildReferenceReceiptHtml({
            size,
            logoUrl: config.showLogo && branch.logoUrl ? branch.logoUrl : undefined,
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
            subLocation,
            lines: bill.lines || [],
            subTotal: Number(bill.subTotal || 0),
            discountAmount: Number(bill.discountAmount || 0),
            discountPercent: Number(bill.discountPercent || 0),
            taxAmount: Number(bill.taxAmount || 0),
            taxPercent: Number(bill.taxPercent || 0),
            serviceChargeAmount: Number(bill.serviceChargeAmount || 0),
            serviceChargePercent: Number(bill.serviceChargePercent || 0),
            grandTotal: Number(bill.grandTotal || 0),
            dueAmount: Number(bill.grandTotal || 0),
            customerName: 'Walk-in',
            orderTaker: bill.waiterName || 'Waiter',
            complaintPhone: branch.phone || '03080004248',
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
            logoUrl: config.showLogo && branch.logoUrl ? branch.logoUrl : undefined,
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
            customerAddress: order.customerAddress ? `${order.customerAddress}${order.notes ? ' (Note: ' + order.notes + ')' : ''}` : order.notes,
            orderTaker: order.riderName ? `Rider: ${order.riderName}` : 'Online Ordering',
            complaintPhone: branch.phone || '03080004248',
        });

        this.executePrint(bodyHtml, `Order-${order.orderNo || order.id}`, size);
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
            logoUrl: config.showLogo && branch.logoUrl ? branch.logoUrl : undefined,
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
            orderTaker: this.authService.getPrinterIdentity().name || 'Staff',
            complaintPhone: branch.phone || '03080004248',
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
            <div class="row" style="margin-top: 4px;"><span>Description:</span><span>${expense.description || '—'}</span></div>

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

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

        const triggerPrint = () => {
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
            setTimeout(triggerPrint, 600);
        } else {
            setTimeout(triggerPrint, 250);
        }
    }

    // =========================================================================
    // 1. POS SALES RECEIPT (Cash/Card Checkout)
    // =========================================================================
    public printPosReceipt(receipt: any, paperSize?: ThermalPaperSize): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (receipt.lines || [])
            .map((line: any) => {
                const dealBadge = line.isDeal ? `<span class="badge">DEAL</span>` : '';
                const dealHtml = line.dealItems?.length
                    ? `<div style="font-size: 8.5px; color: #222; margin-top: 1px; padding-left: 3px; border-left: 2px solid #555;">
                        <div style="font-weight: bold; color: #555;">INCLUDES:</div>
                        ${line.dealItems.map((d: string) => `<div>&bull; ${d}</div>`).join('')}
                       </div>`
                    : '';
                const mods = line.modifiers?.length
                    ? `<div style="font-size: 8.5px; color: #555;">Opts: ${line.modifiers.join(', ')}</div>`
                    : '';
                const notes = line.notes
                    ? `<div style="font-size: 8.5px; color: #b91c1c; font-style: italic;">* ${line.notes}</div>`
                    : '';

                return `
                    <tr style="border-bottom: 1px dotted #ccc;">
                        <td style="width: ${is58 ? '14%' : '12%'}; text-align: left; font-weight: bold;">${line.quantity}x</td>
                        <td style="width: ${is58 ? '46%' : '48%'};">
                            <div style="font-weight: 500;">${dealBadge}${line.productName}</div>
                            ${dealHtml}${mods}${notes}
                        </td>
                        <td style="width: ${is58 ? '20%' : '20%'}; text-align: right; padding-right: 5px;">${Number(line.unitPrice).toFixed(2)}</td>
                        <td style="width: ${is58 ? '20%' : '20%'}; text-align: right; font-weight: bold;">${Number(line.lineTotal).toFixed(2)}</td>
                    </tr>
                `;
            })
            .join('');

        const discountRow =
            receipt.discountAmount > 0
                ? `<div class="row"><span>Discount${receipt.discountPercent > 0 ? ' (' + receipt.discountPercent + '%)' : ''}:</span><span>-${Number(receipt.discountAmount).toFixed(2)}</span></div>`
                : '';
        const taxRow =
            receipt.taxAmount > 0
                ? `<div class="row"><span>Tax (${receipt.taxPercent}%):</span><span>+${Number(receipt.taxAmount).toFixed(2)}</span></div>`
                : '';
        const serviceRow =
            receipt.serviceChargeAmount > 0
                ? `<div class="row"><span>Service Charge (${receipt.serviceChargePercent}%):</span><span>+${Number(receipt.serviceChargeAmount).toFixed(2)}</span></div>`
                : '';
        const deliveryRow =
            receipt.deliveryFee > 0
                ? `<div class="row"><span>Delivery Charges:</span><span>+${Number(receipt.deliveryFee).toFixed(2)}</span></div>`
                : '';

        const locationMeta = receipt.carNumber
            ? `Car: <strong>${receipt.carName || ''} (${receipt.carNumber})</strong> ${receipt.carLocation ? '&bull; Spot: ' + receipt.carLocation : ''}`
            : `Table: <strong>${receipt.tableNo || 'Takeaway'}</strong>`;

        const printerIdentity = this.authService.getPrinterIdentity();

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                ${branch.taxNumber ? `<div class="meta-text bold">NTN/STRN: ${branch.taxNumber}</div>` : ''}
                ${config.headerText ? `<div class="meta-text" style="font-style:italic;">${config.headerText}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** SALES RECEIPT ***</div>
                <div class="meta-text" style="margin-top:2px;">${locationMeta} &bull; Order: #${receipt.orderNo || receipt.id}</div>
                <div class="meta-text">Server: ${receipt.serverName || 'Staff'} &bull; ${new Date(receipt.date || Date.now()).toLocaleString()}</div>
                <div class="meta-text bold" style="margin-top:1px;">Cashier: ${printerIdentity.name} (${printerIdentity.role})</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '14%' : '12%'}; text-align: left;">Qty</th>
                        <th style="width: ${is58 ? '46%' : '48%'}; text-align: left;">Item</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right; padding-right: 5px;">Price</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row"><span>Subtotal:</span><span>${Number(receipt.subTotal || 0).toFixed(2)}</span></div>
            ${discountRow}
            ${taxRow}
            ${serviceRow}
            ${deliveryRow}

            <div class="row grand-total">
                <span>TOTAL AMOUNT:</span>
                <span>${Number(receipt.grandTotal || receipt.totalAmount || 0).toFixed(2)}</span>
            </div>

            <div class="row"><span>Payment (${receipt.paymentType || 'CASH'}):</span><span>${Number(receipt.paidAmount || receipt.cashAmount || receipt.grandTotal || 0).toFixed(2)}</span></div>
            ${receipt.changeAmount > 0 ? `<div class="row bold"><span>Change Returned:</span><span>${Number(receipt.changeAmount).toFixed(2)}</span></div>` : ''}

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>${branch.footer}</div>
                <div style="margin-top: 3px; font-size: 8px;">* * * END OF RECEIPT * * *</div>
            </div>
        `;

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
                : filterType === 'kitchen'
                ? '*** KITCHEN ORDER TICKET (KOT) ***'
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

        const bodyHtml = `
            <div class="text-center">
                <div class="header-title">${branch.name}</div>
                <div class="double-divider"></div>
                <div class="header-subtitle">${slipTitle}</div>
                <div class="meta-text bold" style="font-size: ${is58 ? '11px' : '13px'}; margin: 3px 0;">
                    ${locationMeta} &bull; Order: #${kot.orderNo || kot.orderId}
                </div>
                <div class="meta-text">Server: ${kot.waiterName || 'Staff'} &bull; ${kot.printTime || new Date().toLocaleTimeString()}</div>
                ${kot.printedByName ? `<div class="meta-text">Sent By: ${kot.printedByName} (${kot.printedByRole || 'Staff'})</div>` : ''}
                <div class="divider"></div>
            </div>

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
            </div>
        `;

        this.executePrint(bodyHtml, `KOT-${kot.orderNo || kot.orderId}`, size);
    }

    // =========================================================================
    // 3. GUEST BILL (Pre-payment Check / Kacha Bill)
    // =========================================================================
    public printGuestBill(bill: any, paperSize?: ThermalPaperSize): void {
        const branch = this.getBranchInfo();
        const config = this.getConfig();
        const size = paperSize || config.paperSize;
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (bill.lines || [])
            .map((line: any) => {
                const dealBadge = line.isDeal ? `<span class="badge">DEAL</span>` : '';
                return `
                    <tr style="border-bottom: 1px dotted #ccc;">
                        <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${line.quantity}x</td>
                        <td style="width: ${is58 ? '48%' : '50%'};">
                            <div>${dealBadge}${line.productName}</div>
                            ${line.kitchenNotes ? `<div style="font-size: 8.5px; color:#b91c1c;">* ${line.kitchenNotes}</div>` : ''}
                        </td>
                        <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(line.unitPrice).toFixed(2)}</td>
                        <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(line.quantity * line.unitPrice).toFixed(2)}</td>
                    </tr>
                `;
            })
            .join('');

        const discountRow =
            bill.discountAmount > 0
                ? `<div class="row"><span>Discount:</span><span>-${Number(bill.discountAmount).toFixed(2)}</span></div>`
                : '';
        const taxRow =
            bill.taxAmount > 0
                ? `<div class="row"><span>Tax (${bill.taxPercent}%):</span><span>+${Number(bill.taxAmount).toFixed(2)}</span></div>`
                : '';
        const serviceRow =
            bill.serviceChargeAmount > 0
                ? `<div class="row"><span>Service Charge (${bill.serviceChargePercent}%):</span><span>+${Number(bill.serviceChargeAmount).toFixed(2)}</span></div>`
                : '';

        const locationMeta = bill.isCarService
            ? `Car: <strong>${bill.carName || ''} (${bill.carNumber || ''})</strong>`
            : `Table: <strong>${bill.tableNo || 'Takeaway'}</strong>`;

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** GUEST CHECK / PRE-BILL ***</div>
                <div class="meta-text" style="margin-top:2px;">${locationMeta} &bull; Order: #${bill.orderNo || 'N/A'}</div>
                <div class="meta-text">Server: ${bill.waiterName || 'Staff'} &bull; ${new Date().toLocaleString()}</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '14%' : '12%'}; text-align: left;">Qty</th>
                        <th style="width: ${is58 ? '46%' : '48%'}; text-align: left;">Item</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right; padding-right: 5px;">Price</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row"><span>Subtotal:</span><span>${Number(bill.subTotal || 0).toFixed(2)}</span></div>
            ${discountRow}
            ${taxRow}
            ${serviceRow}

            <div class="row grand-total">
                <span>ESTIMATED TOTAL:</span>
                <span>${Number(bill.grandTotal || 0).toFixed(2)}</span>
            </div>

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>Thank you for dining with us!</div>
                <div class="bold" style="margin-top: 2px;">* PLEASE PRESENT THIS CHECK AT PAYMENT *</div>
            </div>
        `;

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
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const lines = order.lines || [];
        const itemsHtml = lines
            .map((l: any) => {
                const dealBadge = (l.productName || '').toLowerCase().includes('deal')
                    ? `<span class="badge">DEAL</span>`
                    : '';
                return `
                    <tr style="border-bottom: 1px dotted #ccc;">
                        <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${l.quantity}x</td>
                        <td style="width: ${is58 ? '48%' : '50%'};">
                            <div>${dealBadge}${l.productName || 'Item #' + l.productId}</div>
                            ${l.notes ? `<div style="font-size: 8.5px; color:#b91c1c;">* ${l.notes}</div>` : ''}
                        </td>
                        <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(l.unitPrice).toFixed(2)}</td>
                        <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(l.lineTotal || l.quantity * l.unitPrice).toFixed(2)}</td>
                    </tr>
                `;
            })
            .join('');

        const subTotal = order.subTotal || 0;
        const deliveryFee = order.deliveryFee || 0;
        const totalAmount = order.totalAmount || subTotal + deliveryFee;

        const customerInfoHtml = `
            <div style="border: 1px dashed #000; padding: 4px; margin: 4px 0; font-size: ${is58 ? '8.5px' : '10.5px'}; background: #fafafa;">
                <div class="bold uppercase" style="border-bottom: 1px dotted #666; margin-bottom: 2px;">CUSTOMER & DELIVERY DETAILS</div>
                <div><strong>Name:</strong> ${order.customerName || 'Customer'}</div>
                ${order.customerPhone ? `<div><strong>Phone:</strong> ${order.customerPhone}</div>` : ''}
                ${order.deliveryAreaName ? `<div><strong>Area:</strong> ${order.deliveryAreaName}</div>` : ''}
                ${order.customerAddress ? `<div><strong>Address:</strong> ${order.customerAddress}</div>` : ''}
                ${order.notes ? `<div style="font-style: italic; color:#b91c1c;"><strong>Note:</strong> ${order.notes}</div>` : ''}
                ${
                    order.riderName
                        ? `<div class="bold" style="margin-top: 2px; border-top: 1px dotted #666; padding-top: 2px;">Rider: ${order.riderName} ${order.riderPhone ? '(' + order.riderPhone + ')' : ''}</div>`
                        : ''
                }
            </div>
        `;

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** ONLINE DELIVERY ORDER ***</div>
                <div class="meta-text bold" style="margin-top: 2px;">Order #: ${order.orderNo || '#' + order.id}</div>
                ${invoiceNo ? `<div class="meta-text">Invoice #: ${invoiceNo}</div>` : ''}
                <div class="meta-text">Date: ${new Date(order.orderDate).toLocaleString()}</div>
                <div class="divider"></div>
            </div>

            ${customerInfoHtml}

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '14%' : '12%'}; text-align: left;">Qty</th>
                        <th style="width: ${is58 ? '46%' : '48%'}; text-align: left;">Item</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right; padding-right: 5px;">Price</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${itemsHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row"><span>Subtotal:</span><span>PKR ${Number(subTotal).toFixed(2)}</span></div>
            <div class="row"><span>Delivery Fee:</span><span>${deliveryFee > 0 ? 'PKR ' + Number(deliveryFee).toFixed(2) : 'FREE'}</span></div>

            <div class="row grand-total">
                <span>TOTAL AMOUNT:</span>
                <span>PKR ${Number(totalAmount).toFixed(2)}</span>
            </div>

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>Thank you for ordering with us!</div>
                <div style="margin-top: 3px; font-size: 8px;">* * * END OF RECEIPT * * *</div>
            </div>
        `;

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
        const is58 = size === '58mm';

        const logoHtml =
            config.showLogo && branch.logoUrl
                ? `<img src="${branch.logoUrl}" class="logo-img" alt="Logo" />`
                : '';

        const linesHtml = (sale.lines || [])
            .map(
                (l) => `
                <tr style="border-bottom: 1px dotted #ccc;">
                    <td style="width: ${is58 ? '16%' : '14%'}; text-align: center; font-weight: bold;">${l.quantity}x</td>
                    <td style="width: ${is58 ? '48%' : '50%'};">${l.productName || 'Product #' + l.productId}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right;">${Number(l.unitPrice).toFixed(2)}</td>
                    <td style="width: ${is58 ? '18%' : '18%'}; text-align: right; font-weight: bold;">${Number(l.lineTotal).toFixed(2)}</td>
                </tr>
            `
            )
            .join('');

        const paymentName =
            sale.paymentType === 0
                ? 'CASH'
                : sale.paymentType === 1
                ? 'CARD'
                : sale.paymentType === 2
                ? 'CREDIT'
                : 'SPLIT';

        const bodyHtml = `
            <div class="text-center">
                ${logoHtml}
                <div class="header-title">${branch.name}</div>
                ${branch.address ? `<div class="meta-text">${branch.address}</div>` : ''}
                ${branch.phone ? `<div class="meta-text">Tel: ${branch.phone}</div>` : ''}
                ${branch.taxNumber ? `<div class="meta-text bold">NTN/STRN: ${branch.taxNumber}</div>` : ''}
                <div class="double-divider"></div>
                <div class="header-subtitle uppercase">*** SALE INVOICE ***</div>
                <div class="meta-text bold">INVOICE: ${sale.invoiceNo || '#' + sale.id}</div>
                <div class="meta-text">Date: ${new Date(sale.saleDate).toLocaleString()}</div>
                <div class="meta-text">Customer: ${sale.customerName || 'Walk-in Customer'}</div>
                <div class="divider"></div>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: ${is58 ? '14%' : '12%'}; text-align: left;">Qty</th>
                        <th style="width: ${is58 ? '46%' : '48%'}; text-align: left;">Item</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right; padding-right: 5px;">Price</th>
                        <th style="width: ${is58 ? '20%' : '20%'}; text-align: right;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${linesHtml}
                </tbody>
            </table>

            <div class="divider"></div>

            <div class="row"><span>Subtotal:</span><span>${Number(sale.subTotal || 0).toFixed(2)}</span></div>
            ${sale.discountAmount > 0 ? `<div class="row"><span>Discount:</span><span>-${Number(sale.discountAmount).toFixed(2)}</span></div>` : ''}
            ${sale.taxAmount > 0 ? `<div class="row"><span>Tax (${sale.taxPercent}%):</span><span>+${Number(sale.taxAmount).toFixed(2)}</span></div>` : ''}
            ${sale.deliveryCharges ? `<div class="row"><span>Delivery:</span><span>+${Number(sale.deliveryCharges).toFixed(2)}</span></div>` : ''}

            <div class="row grand-total">
                <span>TOTAL AMOUNT:</span>
                <span>${Number(sale.totalAmount || 0).toFixed(2)}</span>
            </div>

            <div class="row"><span>Paid By (${paymentName}):</span><span>${Number(sale.cashAmount || sale.cardAmount || sale.totalAmount).toFixed(2)}</span></div>

            <div class="double-divider"></div>
            <div class="text-center meta-text" style="margin-top: 5px;">
                <div>${branch.footer}</div>
                <div style="margin-top: 3px; font-size: 8px;">* * * END OF INVOICE * * *</div>
            </div>
        `;

        this.executePrint(bodyHtml, `Sale-${sale.invoiceNo || sale.id}`, size);
    }

    // =========================================================================
    // 6. SALE RETURN RECEIPT
    // =========================================================================
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
                <div class="meta-text bold" style="margin-top: 3px;">Paper Profile: ${size} Thermal</div>
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

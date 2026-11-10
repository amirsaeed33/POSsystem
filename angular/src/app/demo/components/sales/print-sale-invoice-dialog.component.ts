import { AppFormatService } from 'src/app/demo/service/app-format.service';
import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { BranchDto } from 'src/app/demo/api/branch';
import { SaleDto } from 'src/app/demo/api/sale';
import { BranchService } from 'src/app/demo/service/branch.service';
import { SaleService } from 'src/app/demo/service/sale.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';

export type InvoicePrintFormat = 'thermal-80' | 'thermal-58' | 'a4';

@Component({
    selector: 'app-print-sale-invoice-dialog',
    templateUrl: './print-sale-invoice-dialog.component.html',
    styleUrls: ['./print-sale-invoice-dialog.component.css'],
})
export class PrintSaleInvoiceDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() saleId: number | null = null;
    @Input() autoPrint = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    sale: SaleDto | null = null;
    company: BranchDto | null = null;
    loading = false;
    today = new Date().toLocaleString();

    printFormat: InvoicePrintFormat =
        (localStorage.getItem('smartpos_invoice_print_format') as InvoicePrintFormat) || 'thermal-80';

    constructor(
        public formatService: AppFormatService,
        private saleService: SaleService,
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private authService: AuthService,
        private thermalPrintService: ThermalPrintService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get printerText(): string {
        return this.authService.getPrinterIdentity().text;
    }

    get printerName(): string {
        return this.authService.getPrinterIdentity().name || 'Staff';
    }

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
    }

    get paymentTypeName(): string {
        switch (this.sale?.paymentType) {
            case 0:
                return 'CASH';
            case 1:
                return 'CARD';
            case 2:
                return 'CREDIT';
            case 3:
                return 'SPLIT (CASH/CARD)';
            default:
                return 'CASH';
        }
    }

    get paymentStatusLabel(): string {
        const due = Number(this.sale?.dueAmount ?? 0);
        if (due > 0.009) {
            return 'Unpaid';
        }
        if (this.sale?.paymentType === 2) {
            return 'Credit';
        }
        return 'Paid';
    }

    get orderTypeLabel(): string {
        const notes = (this.sale?.notes || '').toLowerCase();
        if (notes.includes('takeaway') || notes.includes('take-away') || notes.includes('take away')) {
            return 'Take-Away';
        }
        if (notes.includes('dine-in') || notes.includes('dine in') || notes.includes('table')) {
            return 'Dine-In';
        }
        if (notes.includes('car service') || notes.includes('car:')) {
            return 'Car Service';
        }
        if (notes.includes('delivery')) {
            return 'Delivery';
        }
        return this.sale?.saleType || 'Sale';
    }

    get formattedSaleDate(): string {
        if (!this.sale?.saleDate) return '';
        const d = new Date(this.sale.saleDate);
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = String(d.getFullYear()).slice(-2);
        let hours = d.getHours();
        const minutes = String(d.getMinutes()).padStart(2, '0');
        const ampm = hours >= 12 ? 'pm' : 'am';
        hours = hours % 12 || 12;
        const hStr = String(hours).padStart(2, '0');
        return `${day}/${month}/${year} ${hStr}:${minutes}${ampm}`;
    }

    get printDateParts(): { dateStr: string; timeStr: string; fullStr: string } {
        const d = new Date();
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = String(d.getFullYear()).slice(-2);
        let hours = d.getHours();
        const minutes = String(d.getMinutes()).padStart(2, '0');
        const seconds = String(d.getSeconds()).padStart(2, '0');
        const ampm = hours >= 12 ? 'pm' : 'am';
        hours = hours % 12 || 12;
        const hStr = String(hours).padStart(2, '0');
        const dateStr = `${day}/${month}/${year}`;
        const timeStr = `${hStr}:${minutes}:${seconds}${ampm}`;
        return {
            dateStr,
            timeStr,
            fullStr: `${dateStr} ${timeStr}`,
        };
    }

    get uanLine(): string {
        const phone = (this.company?.invoiceContactPhone || '03080004248').trim();
        if (!phone) {
            return 'UAN';
        }
        return /^uan\b/i.test(phone) ? phone : `UAN ${phone}`;
    }

    get softwareByLine(): string {
        const raw = (this.company?.softwareDeveloperName || 'Software By AlmahirHub').trim();
        if (/^software\s+by\b/i.test(raw)) {
            return raw;
        }
        if (/^software\s+develop/i.test(raw)) {
            return raw.replace(/^software\s+develop(?:ed)?\s+by\b/i, 'Software By');
        }
        return `Software By ${raw}`;
    }

    get softwareDeveloperPhone(): string {
        return (this.company?.softwareDeveloperPhone || '03167854606').trim();
    }
    get currencyLabel(): string {
        return this.company?.currencyFormat || 'Rs';
    }

    get cityOrAddressLine(): string {
        const addr = (this.company?.invoiceAddress || '').trim();
        if (!addr) {
            return '';
        }
        const parts = addr.split(',').map((p) => p.trim()).filter(Boolean);
        return parts.length ? parts[parts.length - 1] : addr;
    }

    setPrintFormat(format: InvoicePrintFormat): void {
        this.printFormat = format;
        localStorage.setItem('smartpos_invoice_print_format', format);
    }

    ngOnChanges(changes: SimpleChanges): void {
        const becameVisible = changes['visible']?.currentValue === true;
        const saleIdChanged = !!changes['saleId'] && this.visible;
        if ((becameVisible || saleIdChanged) && this.visible && this.saleId) {
            this.load(this.saleId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.sale = null;
            this.company = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    print(): void {
        if (!this.sale) return;

        if (this.printFormat === 'thermal-80' || this.printFormat === 'thermal-58') {
            const paperSize = this.printFormat === 'thermal-58' ? '58mm' : '80mm';
            this.thermalPrintService.printSaleInvoice(this.sale, this.company, paperSize);
        } else {
            // A4 print
            setTimeout(() => {
                document.body.classList.add('printing-invoice');
                const cleanup = () => {
                    document.body.classList.remove('printing-invoice');
                    window.removeEventListener('afterprint', cleanup);
                };
                window.addEventListener('afterprint', cleanup);
                window.print();
                setTimeout(cleanup, 1200);
            }, 100);
        }
    }

    private load(id: number): void {
        this.loading = true;
        this.sale = null;
        this.company = null;
        this.today = new Date().toLocaleString();

        Promise.all([
            this.saleService.get(id),
            this.branchService.getInvoiceInfo(this.branchContext.getBranchId()),
        ])
            .then(([sale, company]) => {
                this.sale = sale;
                this.company = company;
                this.loading = false;
                this.cd.detectChanges();

                if (this.autoPrint) {
                    setTimeout(() => this.print(), 250);
                }
            })
            .catch((error) => {
                this.loading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load invoice details',
                });
                this.onHide();
            });
    }
}

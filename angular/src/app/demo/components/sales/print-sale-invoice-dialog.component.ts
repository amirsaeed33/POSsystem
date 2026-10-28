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
    printFormat: 'thermal' | 'a4' = (localStorage.getItem('smartpos_invoice_print_format') as 'thermal' | 'a4') || 'thermal';

    constructor(public formatService: AppFormatService, 
        private saleService: SaleService,
        private branchService: BranchService,
        private authService: AuthService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get printerText(): string {
        return this.authService.getPrinterIdentity().text;
    }

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
    }

    get paymentTypeName(): string {
        switch (this.sale?.paymentType) {
            case 0: return 'CASH';
            case 1: return 'CARD';
            case 2: return 'CREDIT';
            case 3: return 'SPLIT (CASH/CARD)';
            default: return 'CASH';
        }
    }

    setPrintFormat(format: 'thermal' | 'a4'): void {
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
        setTimeout(() => {
            const isThermal = this.printFormat === 'thermal';
            const printClass = isThermal ? 'printing-thermal' : 'printing-invoice';
            document.body.classList.add(printClass);

            let styleEl: HTMLStyleElement | null = null;
            if (isThermal) {
                styleEl = document.createElement('style');
                styleEl.id = 'thermal-print-page-style';
                styleEl.innerHTML = '@page { size: 80mm auto; margin: 0; }';
                document.head.appendChild(styleEl);
            }

            const cleanup = () => {
                document.body.classList.remove('printing-thermal');
                document.body.classList.remove('printing-invoice');
                if (styleEl && styleEl.parentNode) {
                    styleEl.parentNode.removeChild(styleEl);
                }
                window.removeEventListener('afterprint', cleanup);
            };

            window.addEventListener('afterprint', cleanup);
            window.print();
            setTimeout(cleanup, 1200);
        }, 100);
    }

    private load(id: number): void {
        this.loading = true;
        this.sale = null;
        this.company = null;
        this.today = new Date().toLocaleString();

        Promise.all([
            this.saleService.get(id),
            this.branchService.getInvoiceInfo(),
        ])
            .then(([sale, company]) => {
                this.sale = sale;
                this.company = company;
                this.loading = false;
                this.cd.detectChanges();
                if (this.autoPrint) {
                    this.print();
                }
            })
            .catch((error) => {
                this.loading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sale invoice',
                });
                this.onHide();
            });
    }
}

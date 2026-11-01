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
import {
    PurchaseDto,
    PurchasePaymentHistoryDto,
} from 'src/app/demo/api/purchase';
import { BranchService } from 'src/app/demo/service/branch.service';
import { PurchaseService } from 'src/app/demo/service/purchase.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';

export type PurchasePrintFormat = 'thermal-80' | 'thermal-58' | 'a4';

@Component({
    selector: 'app-print-purchase-invoice-dialog',
    templateUrl: './print-purchase-invoice-dialog.component.html',
    styleUrls: ['./print-purchase-invoice-dialog.component.css'],
})
export class PrintPurchaseInvoiceDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() purchaseId: number | null = null;
    @Input() autoPrint = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    purchase: PurchaseDto | null = null;
    company: BranchDto | null = null;
    paymentHistory: PurchasePaymentHistoryDto[] = [];
    loading = false;
    today = new Date().toLocaleString();

    printFormat: PurchasePrintFormat =
        (localStorage.getItem('smartpos_purchase_print_format') as PurchasePrintFormat) || 'thermal-80';

    get isRestaurant(): boolean {
        return this.branchContext.isRestaurant();
    }

    get paymentHistoryTotal(): number {
        return (this.paymentHistory || []).reduce(
            (sum, p) => sum + (Number(p.amount) || 0),
            0
        );
    }

    get dueAmount(): number {
        const total = Number(this.purchase?.totalAmount) || 0;
        const paid =
            this.paymentHistoryTotal ||
            Number(this.purchase?.amountPaid) ||
            0;
        return Math.max(0, Math.round((total - paid) * 100) / 100);
    }

    constructor(
        public formatService: AppFormatService,
        private purchaseService: PurchaseService,
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private thermalPrintService: ThermalPrintService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
    }

    setPrintFormat(format: PurchasePrintFormat): void {
        this.printFormat = format;
        localStorage.setItem('smartpos_purchase_print_format', format);
    }

    ngOnChanges(changes: SimpleChanges): void {
        const becameVisible = changes['visible']?.currentValue === true;
        const purchaseIdChanged = !!changes['purchaseId'] && this.visible;
        if ((becameVisible || purchaseIdChanged) && this.visible && this.purchaseId) {
            this.load(this.purchaseId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.purchase = null;
            this.company = null;
            this.paymentHistory = [];
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    print(): void {
        if (!this.purchase) return;

        if (this.printFormat === 'thermal-80' || this.printFormat === 'thermal-58') {
            const paperSize = this.printFormat === 'thermal-58' ? '58mm' : '80mm';
            this.thermalPrintService.printPurchaseInvoice(this.purchase, this.company, paperSize);
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
                setTimeout(cleanup, 1000);
            }, 100);
        }
    }

    private load(id: number): void {
        this.loading = true;
        this.purchase = null;
        this.company = null;
        this.paymentHistory = [];
        this.today = new Date().toLocaleString();

        const branchId = this.branchContext.getBranchId();
        const contextBranch = this.branchContext.getCurrentBranch();
        if (contextBranch) {
            this.company = contextBranch;
        }

        Promise.all([
            this.purchaseService.get(id),
            this.branchService.getInvoiceInfo(branchId),
            this.purchaseService.getPaymentHistory(id),
        ])
            .then(([purchase, company, payments]) => {
                this.purchase = purchase;
                this.company = company || contextBranch || this.company;
                this.paymentHistory = payments || [];
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
                    detail: error?.message || 'Failed to load purchase invoice',
                });
                this.onHide();
            });
    }
}

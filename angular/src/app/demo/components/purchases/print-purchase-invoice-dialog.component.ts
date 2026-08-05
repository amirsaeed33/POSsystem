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
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { BranchService } from 'src/app/demo/service/branch.service';
import { PurchaseService } from 'src/app/demo/service/purchase.service';

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
    loading = false;
    today = new Date().toLocaleString();

    constructor(
        private purchaseService: PurchaseService,
        private branchService: BranchService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
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
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    print(): void {
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

    private load(id: number): void {
        this.loading = true;
        this.purchase = null;
        this.company = null;
        this.today = new Date().toLocaleString();

        Promise.all([
            this.purchaseService.get(id),
            this.branchService.getInvoiceInfo(),
        ])
            .then(([purchase, company]) => {
                this.purchase = purchase;
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
                    detail: error?.message || 'Failed to load purchase invoice',
                });
                this.onHide();
            });
    }
}

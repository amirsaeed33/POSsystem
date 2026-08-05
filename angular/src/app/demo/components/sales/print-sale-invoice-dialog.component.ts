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

    constructor(
        private saleService: SaleService,
        private branchService: BranchService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
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

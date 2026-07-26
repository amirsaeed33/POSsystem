import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { CompanyProfileDto } from 'src/app/demo/api/company-profile';
import { PurchaseService } from 'src/app/demo/service/purchase.service';
import { CompanyProfileService } from 'src/app/demo/service/company-profile.service';

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
    company: CompanyProfileDto | null = null;
    loading = false;
    today = new Date().toLocaleString();

    constructor(
        private purchaseService: PurchaseService,
        private companyProfileService: CompanyProfileService,
        private messageService: MessageService
    ) {}

    get companyLogoUrl(): string {
        return this.companyProfileService.getImageUrl(this.company?.imagePath);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.purchaseId) {
            this.load(this.purchaseId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.purchase = null;
            this.company = null;
            this.autoPrint = false;
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
            this.companyProfileService.getCurrent(),
        ])
            .then(([purchase, company]) => {
                this.purchase = purchase;
                this.company = company;
                if (this.autoPrint) {
                    this.print();
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchase invoice',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

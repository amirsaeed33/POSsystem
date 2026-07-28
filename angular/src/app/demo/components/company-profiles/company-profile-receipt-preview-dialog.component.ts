import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { CompanyProfileDto } from 'src/app/demo/api/company-profile';
import { CompanyProfileService } from 'src/app/demo/service/company-profile.service';

@Component({
    selector: 'app-company-profile-receipt-preview-dialog',
    templateUrl: './company-profile-receipt-preview-dialog.component.html',
    styleUrls: ['./company-profile-receipt-preview-dialog.component.css'],
})
export class CompanyProfileReceiptPreviewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() profile: CompanyProfileDto | null = null;
    /** Optional data-URL / path override (e.g. unsaved form image preview). */
    @Input() logoPreviewUrl = '';
    @Output() visibleChange = new EventEmitter<boolean>();

    today = new Date().toLocaleString();
    sampleDate = new Date();

    readonly sampleLines = [
        { name: 'Sample Product A', quantity: 2, unitPrice: 500, lineTotal: 1000 },
        { name: 'Sample Product B', quantity: 1, unitPrice: 750, lineTotal: 750 },
        { name: 'Sample Product C', quantity: 3, unitPrice: 120, lineTotal: 360 },
    ];

    readonly sampleSubTotal = 2110;
    readonly sampleDiscount = 110;
    readonly sampleTaxPercent = 5;
    readonly sampleTax = 100;
    readonly sampleTotal = 2100;

    constructor(private companyProfileService: CompanyProfileService) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible']?.currentValue === true) {
            this.today = new Date().toLocaleString();
            this.sampleDate = new Date();
        }
    }

    get companyLogoUrl(): string {
        if (this.logoPreviewUrl) {
            return this.logoPreviewUrl;
        }
        return this.companyProfileService.getImageUrl(this.profile?.imagePath);
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
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
}

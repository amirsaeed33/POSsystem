import { Injectable } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BranchContextService } from './branch-context.service';

@Injectable({ providedIn: 'root' })
export class AppFormatService {
    private readonly datePipe = new DatePipe('en-US');

    constructor(private branchContext: BranchContextService) {}

    get dateFormat(): string {
        return this.branchContext.getCurrentBranch()?.dateFormat || 'dd/MM/yyyy';
    }

    get primeNgDateFormat(): string {
        const format = this.dateFormat;
        if (format === 'dd/MM/yyyy') return 'dd/mm/yy';
        if (format === 'MM/dd/yyyy') return 'mm/dd/yy';
        if (format === 'yyyy-MM-dd') return 'yy-mm-dd';
        if (format === 'dd-MM-yyyy') return 'dd-mm-yy';
        if (format === 'dd MMM yyyy') return 'dd M yy';
        if (format === 'MMM d, y') return 'M d, yy';
        if (format === 'MMMM d, y') return 'MM d, yy';
        if (format === 'EEEE, MMMM d, y') return 'DD, MM d, yy';
        return 'dd/mm/yy'; // Default
    }

    formatDate(value?: string | Date | null, fallback = '—'): string {
        if (!value) {
            return fallback;
        }
        const date = value instanceof Date ? value : new Date(value);
        if (isNaN(date.getTime())) {
            return fallback;
        }
        return this.datePipe.transform(date, this.dateFormat) || fallback;
    }

    get currencyFormat(): string {
        return this.branchContext.getCurrentBranch()?.currencyFormat || 'PKR';
    }
}

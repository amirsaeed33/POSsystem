import { Injectable } from '@angular/core';
import { BranchContextService } from './branch-context.service';

@Injectable({ providedIn: 'root' })
export class AppFormatService {
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

    get currencyFormat(): string {
        return this.branchContext.getCurrentBranch()?.currencyFormat || 'PKR';
    }
}

import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    CreateLedgerEntryDto,
    LedgerEntryDto,
} from 'src/app/demo/api/ledger-entry';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { LedgerEntryService } from 'src/app/demo/service/ledger-entry.service';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';

@Component({
    selector: 'app-ledger-entry-form-dialog',
    templateUrl: './ledger-entry-form-dialog.component.html',
})
export class LedgerEntryFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() entryId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    entry: CreateLedgerEntryDto = this.emptyEntry();
    accounts: BusinessAccountDto[] = [];
    saving = false;
    loading = false;

    voucherTypes = [
        { label: 'Invoice', value: 'Invoice' },
        { label: 'Payment', value: 'Payment' },
        { label: 'Journal', value: 'Journal' },
        { label: 'Opening Balance', value: 'OpeningBalance' },
        { label: 'Adjustment', value: 'Adjustment' },
    ];

    constructor(
        private ledgerEntryService: LedgerEntryService,
        private businessAccountService: BusinessAccountService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.entryId ? 'Edit Ledger Entry' : 'Create Ledger Entry';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadFormData();
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    save(): void {
        if (!this.entry.accountId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Account is required',
            });
            return;
        }
        if (!this.entry.transactionDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Transaction date is required',
            });
            return;
        }
        if (!this.entry.voucherType) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Voucher type is required',
            });
            return;
        }

        const debit = this.entry.debit || 0;
        const credit = this.entry.credit || 0;
        if (debit < 0 || credit < 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Debit and credit must be zero or greater',
            });
            return;
        }
        if (debit === 0 && credit === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Exactly one of debit or credit must be greater than zero',
            });
            return;
        }
        if (debit > 0 && credit > 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Only one of debit or credit can be greater than zero',
            });
            return;
        }

        this.saving = true;
        const payload = {
            accountId: this.entry.accountId,
            transactionDate: this.entry.transactionDate,
            voucherType: this.entry.voucherType,
            voucherId: this.entry.voucherId || undefined,
            debit,
            credit,
            description: this.entry.description?.trim() || undefined,
        };

        const request = this.entryId
            ? this.ledgerEntryService.update({
                  id: this.entryId,
                  ...payload,
              } as LedgerEntryDto)
            : this.ledgerEntryService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.entryId
                        ? 'Ledger entry updated successfully'
                        : 'Ledger entry created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save ledger entry',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyEntry(): CreateLedgerEntryDto {
        return {
            accountId: null as any,
            transactionDate: this.toDateInputValue(),
            voucherType: 'Journal',
            voucherId: undefined,
            debit: 0,
            credit: 0,
            description: '',
        };
    }

    private resetForm(): void {
        this.entry = this.emptyEntry();
        this.saving = false;
        this.loading = false;
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private toDateInputFromValue(value?: string | Date): string {
        if (!value) {
            return this.toDateInputValue();
        }
        const date = value instanceof Date ? value : new Date(value);
        if (isNaN(date.getTime())) {
            return typeof value === 'string' ? value.substring(0, 10) : this.toDateInputValue();
        }
        return this.toDateInputValue(date);
    }

    private loadFormData(): void {
        this.loading = true;
        const accountsPromise = this.businessAccountService.getAll({
            skipCount: 0,
            maxResultCount: 1000,
        });
        const entryPromise = this.entryId
            ? this.ledgerEntryService.get(this.entryId)
            : Promise.resolve(null as LedgerEntryDto | null);

        Promise.all([accountsPromise, entryPromise])
            .then(([accounts, entry]) => {
                this.accounts = accounts.items;
                if (entry) {
                    this.entry = {
                        accountId: entry.accountId,
                        transactionDate: this.toDateInputFromValue(
                            entry.transactionDate
                        ),
                        voucherType: entry.voucherType,
                        voucherId: entry.voucherId,
                        debit: entry.debit || 0,
                        credit: entry.credit || 0,
                        description: entry.description || '',
                    };
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load form data',
                });
                if (this.entryId) {
                    this.onHide();
                }
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

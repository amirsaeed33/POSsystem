import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    BusinessAccountDto,
    CreateBusinessAccountDto,
} from 'src/app/demo/api/business-account';
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';
import { LookUpService } from 'src/app/demo/service/lookup.service';

@Component({
    selector: 'app-account-form-dialog',
    templateUrl: './account-form-dialog.component.html',
})
export class AccountFormDialogComponent implements OnInit, OnChanges {
    @Input() visible = false;
    @Input() accountId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    account: BusinessAccountDto = this.emptyAccount();
    saving = false;
    loading = false;

    accountTypeLookups: LookUpDto[] = [];

    constructor(
        private businessAccountService: BusinessAccountService,
        private lookupService: LookUpService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.loadAccountTypeLookups();
    }

    private loadAccountTypeLookups(): void {
        this.lookupService
            .getByType(LookUpTypes.AccountType)
            .then((res: LookUpDto[]) => {
                this.accountTypeLookups = res || [];
            })
            .catch(() => {
                this.accountTypeLookups = [];
            });
    }

    get dialogTitle(): string {
        return this.accountId ? 'Edit Account' : 'Create Account';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadAccountTypeLookups();
            if (this.accountId) {
                this.loadAccount(this.accountId);
            }
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
        const name = (this.account.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const payload: CreateBusinessAccountDto = {
            name,
            code: this.account.code?.trim() || undefined,
            accountType: this.account.accountType || undefined,
            accountTypeId: this.account.accountTypeId || undefined,
            openingBalance: this.account.openingBalance ?? 0,
            description: this.account.description?.trim() || undefined,
            isActive: this.account.isActive !== false,
        };

        const request = this.accountId
            ? this.businessAccountService.update({
                  id: this.accountId,
                  ...payload,
                  balance: this.account.balance ?? 0,
              })
            : this.businessAccountService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.accountId
                        ? 'Account updated successfully'
                        : 'Account created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save account',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyAccount(): BusinessAccountDto {
        return {
            id: 0,
            name: '',
            code: '',
            accountType: undefined,
            accountTypeId: undefined,
            accountTypeName: undefined,
            openingBalance: 0,
            balance: 0,
            description: '',
            isActive: true,
        };
    }

    private resetForm(): void {
        this.account = this.emptyAccount();
        this.saving = false;
        this.loading = false;
    }

    private loadAccount(id: number): void {
        this.loading = true;
        this.businessAccountService
            .get(id)
            .then((account) => {
                this.account = { ...account };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load account',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

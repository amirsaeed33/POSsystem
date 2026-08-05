import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { MessageService } from 'primeng/api';
import { CreateLookUpDto, LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { LookUpService } from 'src/app/demo/service/lookup.service';

@Component({
    selector: 'app-lookup-form-dialog',
    templateUrl: './lookup-form-dialog.component.html',
})
export class LookUpFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() id: number | null = null;
    @Input() defaultType: string | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    typeOptions: { label: string; value: string }[] = [];
    type: string = LookUpTypes.PaymentMethod;
    name = '';
    displayName = '';
    sortOrder = 0;
    isActive = true;
    saving = false;
    loading = false;

    constructor(
        private lookUpService: LookUpService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.id ? 'Edit Lookup' : 'Create Lookup';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadTypeOptions().then(() => {
                if (this.id) {
                    this.loadItem(this.id);
                } else if (!this.type && this.typeOptions.length) {
                    this.type = this.defaultType || this.typeOptions[0].value;
                }
            });
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
        const name = (this.name || '').trim();
        const displayName = (this.displayName || '').trim();
        if (!this.type || !name || !displayName) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Type, name, and display name are required.',
            });
            return;
        }

        this.saving = true;
        const payload: CreateLookUpDto = {
            type: this.type,
            name,
            displayName,
            sortOrder: this.sortOrder ?? 0,
            isActive: this.isActive,
        };

        const request = this.id
            ? this.lookUpService.update({ ...payload, id: this.id } as LookUpDto)
            : this.lookUpService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.id ? 'Lookup updated successfully' : 'Lookup created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save lookup',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.type = this.defaultType || LookUpTypes.PaymentMethod;
        this.name = '';
        this.displayName = '';
        this.sortOrder = 0;
        this.isActive = true;
        this.saving = false;
        this.loading = false;
    }

    private loadTypeOptions(): Promise<void> {
        return this.lookUpService
            .getByType(LookUpTypes.LookUpType)
            .then((items) => {
                this.typeOptions = items.map((x) => ({
                    label: x.displayName,
                    value: x.name,
                }));
                if (!this.id) {
                    const preferred = this.typeOptions.find((x) => x.value === this.type);
                    if (!preferred) {
                        this.type = this.defaultType
                            || this.typeOptions.find((x) => x.value === LookUpTypes.PaymentMethod)?.value
                            || this.typeOptions[0]?.value
                            || '';
                    }
                }
            })
            .catch((error) => {
                this.typeOptions = [];
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load lookup types',
                });
            });
    }

    private loadItem(id: number): void {
        this.loading = true;
        this.lookUpService
            .get(id)
            .then((item) => {
                this.type = item.type;
                this.name = item.name;
                this.displayName = item.displayName;
                this.sortOrder = item.sortOrder ?? 0;
                this.isActive = item.isActive;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load lookup',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

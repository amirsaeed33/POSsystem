import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { LookUpService } from 'src/app/demo/service/lookup.service';

@Component({
    templateUrl: './lookup-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class LookUpListComponent implements OnInit {
    items: LookUpDto[] = [];
    typeOptions: { label: string; value: string }[] = [];
    filterType: string | null = null;
    loading = false;
    keyword = '';
    dialogVisible = false;
    editingId: number | null = null;

    constructor(
        private lookUpService: LookUpService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadTypeOptions().then(() => this.loadItems());
    }

    onTypeFilterChange(): void {
        this.loadItems();
    }

    loadItems(): void {
        this.loading = true;
        this.lookUpService
            .getAll({
                type: this.filterType || undefined,
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.items = result.items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load lookups',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadItems();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingId = null;
        this.dialogVisible = true;
    }

    openEditDialog(item: LookUpDto): void {
        this.editingId = item.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadTypeOptions().then(() => this.loadItems());
    }

    typeLabel(type: string): string {
        return this.typeOptions.find((t) => t.value === type)?.label || type;
    }

    onDelete(item: LookUpDto): void {
        this.confirmationService.confirm({
            message: `Delete lookup "${item.displayName}" (${item.name})?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.lookUpService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Lookup deleted successfully',
                        });
                        this.loadTypeOptions().then(() => this.loadItems());
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete lookup',
                        });
                    });
            },
        });
    }

    private loadTypeOptions(): Promise<void> {
        return this.lookUpService
            .getByType(LookUpTypes.LookUpType)
            .then((items) => {
                this.typeOptions = items.map((x) => ({
                    label: x.displayName,
                    value: x.name,
                }));
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
}

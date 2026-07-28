import { Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { UnitDto } from 'src/app/demo/api/unit';
import { UnitService } from 'src/app/demo/service/unit.service';

@Component({
    selector: 'app-unit-list',
    templateUrl: './unit-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class UnitListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Product Settings tabs). */
    @Input() embedded = false;

    units: UnitDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingUnitId: number | null = null;

    constructor(
        private unitService: UnitService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadUnits();
    }

    loadUnits(): void {
        this.loading = true;
        this.unitService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.units = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load units',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadUnits();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingUnitId = null;
        this.dialogVisible = true;
    }

    openEditDialog(unit: UnitDto): void {
        this.editingUnitId = unit.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadUnits();
    }

    onDelete(unit: UnitDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete unit "${unit.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.unitService
                    .delete(unit.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Unit deleted successfully',
                        });
                        this.loadUnits();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete unit',
                        });
                    });
            },
        });
    }
}

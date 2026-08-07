import { Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { StaffDto } from 'src/app/demo/api/staff';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-list',
    templateUrl: './staff-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StaffListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Staff hub tabs). */
    @Input() embedded = false;

    staffList: StaffDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    statusFilter: boolean | null = null;

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Active', value: true },
        { label: 'Inactive', value: false },
    ];

    dialogVisible = false;
    editingStaffId: number | null = null;

    loginDialogVisible = false;
    loginStaff: StaffDto | null = null;

    constructor(
        private staffService: StaffService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadStaff();
    }

    loadStaff(): void {
        this.loading = true;
        this.staffService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                isActive: this.statusFilter,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.staffList = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load staff',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadStaff();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingStaffId = null;
        this.dialogVisible = true;
    }

    openEditDialog(staff: StaffDto): void {
        this.editingStaffId = staff.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadStaff();
    }

    openLoginDialog(staff: StaffDto): void {
        this.loginStaff = staff;
        this.loginDialogVisible = true;
    }

    onLoginSaved(): void {
        this.loadStaff();
    }

    formatDate(value: string | Date | undefined): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleDateString();
    }

    onDelete(staff: StaffDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete staff "${staff.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.staffService
                    .delete(staff.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Staff deleted successfully',
                        });
                        this.loadStaff();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete staff',
                        });
                    });
            },
        });
    }
}

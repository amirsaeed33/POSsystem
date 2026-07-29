import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { RoleDto } from 'src/app/demo/api/role-management';
import { RoleService } from 'src/app/demo/service/role.service';
import { MessageService, ConfirmationService } from 'primeng/api';

@Component({
    templateUrl: './rolelist.component.html',
    providers: [MessageService, ConfirmationService]
})
export class RoleListComponent implements OnInit {

    roles: RoleDto[] = [];
    loading = false;
    totalRecords = 0;

    dialogVisible = false;
    editingRoleId: number | null = null;

    constructor(
        private roleService: RoleService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.loadRoles();
    }

    loadRoles() {
        this.loading = true;
        this.roleService.getAll({
            skipCount: 0,
            maxResultCount: 1000
        })
            .then((result) => {
                this.roles = result.items;
                this.totalRecords = result.totalCount;
                this.loading = false;
            })
            .catch((error) => {
                this.loading = false;
                const errorMessage = error?.message || 'Failed to load roles. Please try again.';
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: errorMessage 
                });
            });
    }

    onGlobalFilter(table: Table, event: Event) {
        const value = (event.target as HTMLInputElement).value;
        table.filterGlobal(value, 'contains');
    }

    openCreateDialog() {
        this.editingRoleId = null;
        this.dialogVisible = true;
    }

    openEditDialog(role: RoleDto) {
        this.editingRoleId = role.id;
        this.dialogVisible = true;
    }

    onDialogSaved() {
        this.loadRoles();
    }

    onDeleteRole(role: RoleDto) {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete role "${role.displayName}"? This action cannot be undone.`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.roleService.delete(role.id)
                    .then(() => {
                        this.messageService.add({ 
                            severity: 'success', 
                            summary: 'Success', 
                            detail: 'Role deleted successfully' 
                        });
                        this.loadRoles();
                    })
                    .catch((error) => {
                        this.messageService.add({ 
                            severity: 'error', 
                            summary: 'Error', 
                            detail: error?.message || 'Failed to delete role' 
                        });
                    });
            }
        });
    }
}

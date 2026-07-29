import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { UserDto } from 'src/app/demo/api/user-management';
import { UserService } from 'src/app/demo/service/user.service';
import { MessageService, ConfirmationService } from 'primeng/api';
import { environment } from 'src/environments/environment';

@Component({
    templateUrl: './profilelist.component.html',
    providers: [MessageService, ConfirmationService]
})
export class ProfileListComponent implements OnInit {

    users: UserDto[] = [];
    loading = false;
    totalRecords = 0;

    dialogVisible = false;
    editingUserId: number | null = null;

    resetDialogVisible = false;
    resetUserId: number | null = null;
    resetUserName = '';

    constructor(
        private userService: UserService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.loadUsers();
    }

    loadUsers() {
        this.loading = true;
        this.userService.getAll({
            skipCount: 0,
            maxResultCount: 1000
        })
            .then((result) => {
                this.users = result.items;
                this.totalRecords = result.totalCount;
                this.loading = false;
            })
            .catch((error) => {
                this.loading = false;
                const errorMessage = error?.message || 'Failed to load users. Please try again.';
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
        this.editingUserId = null;
        this.dialogVisible = true;
    }

    openEditDialog(user: UserDto) {
        this.editingUserId = user.id;
        this.dialogVisible = true;
    }

    onDialogSaved() {
        this.loadUsers();
    }

    onResetPassword(user: UserDto) {
        this.resetUserId = user.id;
        this.resetUserName = user.userName;
        this.resetDialogVisible = true;
    }

    onDeleteUser(user: UserDto) {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete user "${user.userName}"? This action cannot be undone.`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.userService.delete(user.id)
                    .then(() => {
                        this.messageService.add({ 
                            severity: 'success', 
                            summary: 'Success', 
                            detail: 'User deleted successfully' 
                        });
                        this.loadUsers();
                    })
                    .catch((error) => {
                        this.messageService.add({ 
                            severity: 'error', 
                            summary: 'Error', 
                            detail: error?.message || 'Failed to delete user' 
                        });
                    });
            }
        });
    }

    onToggleActive(user: UserDto) {
        const action = user.isActive ? this.userService.deactivate(user.id) : this.userService.activate(user.id);
        action
            .then(() => {
                user.isActive = !user.isActive;
                this.messageService.add({ 
                    severity: 'success', 
                    summary: 'Success', 
                    detail: `User ${user.isActive ? 'activated' : 'deactivated'} successfully` 
                });
            })
            .catch((error) => {
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: error?.message || 'Failed to update user status' 
                });
            });
    }

    getProfilePictureUrl(profilePictureUrl: string | undefined): string {
        if (!profilePictureUrl) {
            return 'assets/layout/images/avatar.png';
        }
        
        if (profilePictureUrl.startsWith('http://') || profilePictureUrl.startsWith('https://')) {
            return profilePictureUrl;
        }
        
        return `${environment.apiUrl}${profilePictureUrl.startsWith('/') ? '' : '/'}${profilePictureUrl}`;
    }

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
    }
}

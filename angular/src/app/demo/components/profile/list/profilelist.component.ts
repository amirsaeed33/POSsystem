import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
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

    constructor(
        private userService: UserService,
        private router: Router,
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

    navigateToCreateUser() {
        this.router.navigate(['profile/create']);
    }

    onEditUser(user: UserDto) {
        this.router.navigate(['profile/edit', user.id]);
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
                            detail: 'Failed to delete user' 
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
                    detail: 'Failed to update user status' 
                });
            });
    }

    getRoleNames(user: UserDto): string {
        if (user.roleNames && user.roleNames.length > 0) {
            return user.roleNames.join(', ');
        }
        return 'No roles';
    }

    getProfilePictureUrl(profilePictureUrl: string | undefined): string {
        if (!profilePictureUrl) {
            return 'assets/layout/images/avatar.png';
        }
        
        // If it's already a full URL, use it; otherwise construct from base URL
        if (profilePictureUrl.startsWith('http://') || profilePictureUrl.startsWith('https://')) {
            return profilePictureUrl;
        }
        
        // It's a relative path, construct full URL
        return `${environment.apiUrl}${profilePictureUrl.startsWith('/') ? '' : '/'}${profilePictureUrl}`;
    }

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
    }
}
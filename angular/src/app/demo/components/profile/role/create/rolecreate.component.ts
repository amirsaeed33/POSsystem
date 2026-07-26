import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RoleService } from 'src/app/demo/service/role.service';
import { RoleDto, CreateRoleDto, PermissionDto, GetRoleForEditOutput } from 'src/app/demo/api/role-management';
import { MessageService } from 'primeng/api';

@Component({
    templateUrl: './rolecreate.component.html',
    providers: [MessageService]
})
export class RoleCreateComponent implements OnInit { 

    roleForm: FormGroup;
    permissions: PermissionDto[] = [];
    loading = false;
    loadingPermissions = false;
    selectedPermissions: string[] = [];
    isEditMode = false;
    roleId: number | null = null;
    permissionGroups: { [key: string]: PermissionDto[] } = {};

    constructor(
        private fb: FormBuilder,
        private roleService: RoleService,
        private router: Router,
        private route: ActivatedRoute,
        private messageService: MessageService
    ) {
        this.roleForm = this.fb.group({
            name: ['', [Validators.required]],
            displayName: ['', [Validators.required]],
            description: [''],
            grantedPermissions: [[]]
        });
    }

    ngOnInit() {
        this.loadPermissions();
        
        // Check if we're in edit mode - get id from parent route params
        const parentParams = this.route.snapshot.parent?.params;
        if (parentParams && parentParams['id']) {
            this.isEditMode = true;
            this.roleId = +parentParams['id'];
            this.loadRole(this.roleId);
        }
        
        // Also subscribe to parent route params in case of navigation
        this.route.parent?.params.subscribe(params => {
            if (params['id'] && !this.isEditMode) {
                this.isEditMode = true;
                this.roleId = +params['id'];
                this.loadRole(this.roleId);
            }
        });
    }

    loadRole(id: number) {
        this.loading = true;
        this.loadingPermissions = true;
        this.roleService.getRoleForEdit(id)
            .then((result: GetRoleForEditOutput) => {
                this.permissions = result.permissions || [];
                this.groupPermissions();
                this.roleForm.patchValue({
                    name: result.role.name,
                    displayName: result.role.displayName,
                    description: result.role.description || '',
                    grantedPermissions: result.grantedPermissionNames || []
                });
                this.selectedPermissions = result.grantedPermissionNames || [];
                this.loading = false;
                this.loadingPermissions = false;
            })
            .catch((error) => {
                this.loading = false;
                this.loadingPermissions = false;
                const errorMessage = error?.message || 'Failed to load role';
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: errorMessage 
                });
                this.router.navigate(['profile/role']);
            });
    }

    loadPermissions() {
        this.loadingPermissions = true;
        this.roleService.getAllPermissions()
            .then((permissions) => {
                this.permissions = permissions || [];
                this.groupPermissions();
                this.loadingPermissions = false;
            })
            .catch((error) => {
                this.loadingPermissions = false;
                const errorMessage = error?.message || 'Failed to load permissions';
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: errorMessage 
                });
            });
    }

    groupPermissions() {
        this.permissionGroups = {};
        if (!this.permissions || this.permissions.length === 0) {
            return;
        }
        this.permissions.forEach(permission => {
            if (!permission || !permission.name) {
                return;
            }
            // Use parentName if available, otherwise extract from name or use 'Other'
            let groupName = permission.parentName;
            if (!groupName && permission.name) {
                const parts = permission.name.split('.');
                if (parts.length > 1) {
                    groupName = parts.slice(0, -1).join('.');
                } else {
                    groupName = 'Other';
                }
            }
            groupName = groupName || 'Other';
            
            if (!this.permissionGroups[groupName]) {
                this.permissionGroups[groupName] = [];
            }
            this.permissionGroups[groupName].push(permission);
        });
    }

    getPermissionGroups(): string[] {
        return Object.keys(this.permissionGroups);
    }

    onSubmit() {
        if (this.roleForm.invalid) {
            this.roleForm.markAllAsTouched();
            return;
        }

        this.loading = true;
        const formValue = this.roleForm.value;
        formValue.grantedPermissions = this.selectedPermissions;
        
        if (this.isEditMode && this.roleId) {
            // Update existing role
            formValue.id = this.roleId;
            
            this.roleService.update(formValue)
                .then(() => {
                    this.messageService.add({ 
                        severity: 'success', 
                        summary: 'Success', 
                        detail: 'Role updated successfully' 
                    });
                    this.router.navigate(['profile/role']);
                })
                .catch((error) => {
                    this.loading = false;
                    const errorMessage = error?.message || 'Failed to update role';
                    this.messageService.add({ 
                        severity: 'error', 
                        summary: 'Error', 
                        detail: errorMessage 
                    });
                });
        } else {
            // Create new role
            this.roleService.create(formValue)
                .then(() => {
                    this.messageService.add({ 
                        severity: 'success', 
                        summary: 'Success', 
                        detail: 'Role created successfully' 
                    });
                    this.router.navigate(['profile/role']);
                })
                .catch((error) => {
                    this.loading = false;
                    const errorMessage = error?.message || 'Failed to create role';
                    this.messageService.add({ 
                        severity: 'error', 
                        summary: 'Error', 
                        detail: errorMessage 
                    });
                });
        }
    }

    onCancel() {
        this.router.navigate(['profile/role']);
    }
}


import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RoleService } from 'src/app/demo/service/role.service';
import { RoleDto, CreateRoleDto, PermissionDto, GetRoleForEditOutput } from 'src/app/demo/api/role-management';
import { PermissionMatrixRow } from '../list/role-form-dialog.component';
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
        
        const currentId = this.route.snapshot.params['id'] || this.route.snapshot.parent?.params['id'];
        if (currentId) {
            this.isEditMode = true;
            this.roleId = +currentId;
            this.loadRole(this.roleId);
        }
        
        this.route.params.subscribe(params => {
            if (params['id'] && (!this.isEditMode || this.roleId !== +params['id'])) {
                this.isEditMode = true;
                this.roleId = +params['id'];
                this.loadRole(this.roleId);
            }
        });

        this.route.parent?.params.subscribe(params => {
            if (params['id'] && (!this.isEditMode || this.roleId !== +params['id'])) {
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
                this.buildPermissionMatrix();
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
        if (!this.isEditMode) {
            this.selectedPermissions = [];
        }
        this.roleService.getAllPermissions()
            .then((permissions) => {
                this.permissions = permissions || [];
                if (!this.isEditMode) {
                    this.selectedPermissions = [];
                }
                this.buildPermissionMatrix();
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

    permissionMatrixRows: PermissionMatrixRow[] = [];

    formatGroupName(rawName: string): string {
        if (!rawName) return 'Other';
        let name = rawName.replace(/^Pages\./, '');
        name = name.replace(/([a-z])([A-Z])/g, '$1 $2');
        return name.trim();
    }

    isPermissionGranted(permName?: string): boolean {
        if (!permName) return false;
        return this.selectedPermissions.includes(permName);
    }

    togglePermission(permName?: string): void {
        if (!permName || this.loading) return;
        const index = this.selectedPermissions.indexOf(permName);
        if (index > -1) {
            this.selectedPermissions.splice(index, 1);
        } else {
            this.selectedPermissions.push(permName);
        }
    }

    isRowAllChecked(row: PermissionMatrixRow): boolean {
        const perms = this.getRowPermissions(row);
        return perms.length > 0 && perms.every((p) => this.isPermissionGranted(p.name));
    }

    toggleRowAll(row: PermissionMatrixRow): void {
        if (this.loading) return;
        const perms = this.getRowPermissions(row);
        const allChecked = this.isRowAllChecked(row);
        perms.forEach((p) => {
            if (!p.name) return;
            const index = this.selectedPermissions.indexOf(p.name);
            if (allChecked && index > -1) {
                this.selectedPermissions.splice(index, 1);
            } else if (!allChecked && index === -1) {
                this.selectedPermissions.push(p.name);
            }
        });
    }

    isAllMatrixChecked(): boolean {
        if (!this.permissions.length) return false;
        return this.permissions.every((p) => this.isPermissionGranted(p.name));
    }

    toggleAllMatrix(): void {
        if (this.loading) return;
        const allChecked = this.isAllMatrixChecked();
        if (allChecked) {
            this.selectedPermissions = [];
        } else {
            this.selectedPermissions = this.permissions.map((p) => p.name).filter(Boolean);
        }
    }

    private getRowPermissions(row: PermissionMatrixRow): PermissionDto[] {
        const list: PermissionDto[] = [];
        if (row.viewPerm) list.push(row.viewPerm);
        if (row.createPerm) list.push(row.createPerm);
        if (row.editPerm) list.push(row.editPerm);
        if (row.deletePerm) list.push(row.deletePerm);
        if (row.approvePerm) list.push(row.approvePerm);
        if (row.otherPerms?.length) list.push(...row.otherPerms);
        return list;
    }

    buildPermissionMatrix(): void {
        this.permissionMatrixRows = [];
        if (!this.permissions?.length) return;

        const rowMap = new Map<string, PermissionMatrixRow>();

        this.permissions.forEach((perm) => {
            if (!perm?.name || perm.name === 'Pages') return;

            let groupKey = perm.parentName || '';
            if (!groupKey || groupKey === 'Pages') {
                const parts = perm.name.split('.');
                if (parts.length > 2) {
                    groupKey = parts.slice(0, 2).join('.');
                } else if (parts.length === 2 && parts[0] === 'Pages') {
                    groupKey = perm.name;
                } else {
                    groupKey = parts.length > 1 ? parts.slice(0, -1).join('.') : perm.name;
                }
            }

            if (groupKey === 'Pages') return;

            const cleanFeature = this.formatGroupName(groupKey);
            if (cleanFeature.toLowerCase() === 'pages') return;

            if (!rowMap.has(cleanFeature)) {
                rowMap.set(cleanFeature, {
                    featureName: cleanFeature,
                    otherPerms: []
                });
            }

            const row = rowMap.get(cleanFeature)!;
            const lastPart = perm.name.split('.').pop()?.toLowerCase() || '';

            if (lastPart === 'create') {
                row.createPerm = perm;
            } else if (lastPart === 'edit') {
                row.editPerm = perm;
            } else if (lastPart === 'delete') {
                row.deletePerm = perm;
            } else if (lastPart === 'approve') {
                row.approvePerm = perm;
            } else if (perm.name === groupKey || lastPart === groupKey.split('.').pop()?.toLowerCase()) {
                row.viewPerm = perm;
            } else {
                row.otherPerms.push(perm);
            }
        });

        this.permissionMatrixRows = Array.from(rowMap.values()).sort((a, b) =>
            a.featureName.localeCompare(b.featureName)
        );
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


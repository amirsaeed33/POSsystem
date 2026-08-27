import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import {
    GetRoleForEditOutput,
    PermissionDto,
} from 'src/app/demo/api/role-management';
import { RoleService } from 'src/app/demo/service/role.service';

export interface PermissionMatrixRow {
    featureName: string;
    viewPerm?: PermissionDto;
    createPerm?: PermissionDto;
    editPerm?: PermissionDto;
    deletePerm?: PermissionDto;
    approvePerm?: PermissionDto;
    otherPerms: PermissionDto[];
}

@Component({
    selector: 'app-role-form-dialog',
    templateUrl: './role-form-dialog.component.html',
})
export class RoleFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() roleId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    roleForm: FormGroup;
    permissions: PermissionDto[] = [];
    selectedPermissions: string[] = [];
    permissionMatrixRows: PermissionMatrixRow[] = [];
    loading = false;
    loadingPermissions = false;
    saving = false;

    constructor(
        private fb: FormBuilder,
        private roleService: RoleService,
        private messageService: MessageService
    ) {
        this.roleForm = this.fb.group({
            name: ['', [Validators.required]],
            displayName: ['', [Validators.required]],
            description: [''],
            grantedPermissions: [[]],
        });
    }

    get dialogTitle(): string {
        return this.isEdit ? 'Edit Role' : 'Create Role';
    }

    get isEdit(): boolean {
        return !!this.roleId;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.roleId) {
                this.loadRole(this.roleId);
            } else {
                this.loadPermissions();
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
        if (!permName || this.saving) return;
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
        if (this.saving) return;
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
        if (this.saving) return;
        const allChecked = this.isAllMatrixChecked();
        if (allChecked) {
            this.selectedPermissions = [];
        } else {
            this.selectedPermissions = this.permissions.map((p) => p.name).filter(Boolean);
        }
    }

    onSubmit(): void {
        if (this.roleForm.invalid) {
            this.roleForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        const formValue = this.roleForm.value;
        formValue.grantedPermissions = this.selectedPermissions || [];

        const request =
            this.isEdit && this.roleId
                ? this.roleService.update({ ...formValue, id: this.roleId })
                : this.roleService.create(formValue);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.isEdit
                        ? 'Role updated successfully'
                        : 'Role created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        (this.isEdit
                            ? 'Failed to update role'
                            : 'Failed to create role'),
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.roleForm.reset({
            name: '',
            displayName: '',
            description: '',
            grantedPermissions: [],
        });
        this.selectedPermissions = [];
        this.permissions = [];
        this.permissionMatrixRows = [];
        this.loading = false;
        this.saving = false;
        this.loadingPermissions = false;
    }

    private loadPermissions(): void {
        this.loadingPermissions = true;
        this.selectedPermissions = [];
        this.roleService
            .getAllPermissions()
            .then((permissions) => {
                this.permissions = permissions || [];
                this.selectedPermissions = [];
                this.buildPermissionMatrix();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load permissions',
                });
            })
            .finally(() => {
                this.loadingPermissions = false;
            });
    }

    private loadRole(id: number): void {
        this.loading = true;
        this.loadingPermissions = true;
        this.roleService
            .getRoleForEdit(id)
            .then((result: GetRoleForEditOutput) => {
                this.permissions = result.permissions || [];
                this.buildPermissionMatrix();
                this.roleForm.patchValue({
                    name: result.role.name,
                    displayName: result.role.displayName,
                    description: result.role.description || '',
                    grantedPermissions: result.grantedPermissionNames || [],
                });
                this.selectedPermissions = result.grantedPermissionNames || [];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load role',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
                this.loadingPermissions = false;
            });
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

    private buildPermissionMatrix(): void {
        this.permissionMatrixRows = [];
        if (!this.permissions?.length) return;

        const rowMap = new Map<string, PermissionMatrixRow>();

        this.permissions.forEach((perm) => {
            if (!perm?.name || perm.name === 'Pages') return;

            // Determine the target feature group (e.g. Pages.Users.Create -> Pages.Users, Pages.Products -> Pages.Products)
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
}

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
    permissionGroups: { [key: string]: PermissionDto[] } = {};
    selectedPermissions: string[] = [];
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

    getPermissionGroups(): string[] {
        return Object.keys(this.permissionGroups);
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
        this.permissionGroups = {};
        this.loading = false;
        this.saving = false;
        this.loadingPermissions = false;
    }

    private loadPermissions(): void {
        this.loadingPermissions = true;
        this.roleService
            .getAllPermissions()
            .then((permissions) => {
                this.permissions = permissions || [];
                this.groupPermissions();
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
                this.groupPermissions();
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

    private groupPermissions(): void {
        this.permissionGroups = {};
        if (!this.permissions?.length) {
            return;
        }
        this.permissions.forEach((permission) => {
            if (!permission?.name) {
                return;
            }
            let groupName = permission.parentName;
            if (!groupName && permission.name) {
                const parts = permission.name.split('.');
                groupName =
                    parts.length > 1
                        ? parts.slice(0, -1).join('.')
                        : 'Other';
            }
            groupName = groupName || 'Other';
            if (!this.permissionGroups[groupName]) {
                this.permissionGroups[groupName] = [];
            }
            this.permissionGroups[groupName].push(permission);
        });
    }
}

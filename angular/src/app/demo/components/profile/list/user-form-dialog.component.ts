import {
    Component,
    ElementRef,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { RoleDto } from 'src/app/demo/api/role-management';
import { PermissionDto } from 'src/app/demo/api/role-management';
import { BranchDto } from 'src/app/demo/api/branch';
import { RoleService } from 'src/app/demo/service/role.service';
import { UserService } from 'src/app/demo/service/user.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-user-form-dialog',
    templateUrl: './user-form-dialog.component.html',
})
export class UserFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() userId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    userForm: FormGroup;
    roles: RoleDto[] = [];
    branches: BranchDto[] = [];
    selectedRoles: string[] = [];
    permissions: PermissionDto[] = [];
    permissionGroups: { [key: string]: PermissionDto[] } = {};
    selectedUserPermissions: string[] = [];
    profilePictureUrl: string | null = null;
    profilePictureChanged = false;
    loading = false;
    loadingPermissions = false;
    saving = false;

    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

    constructor(
        private fb: FormBuilder,
        private userService: UserService,
        private roleService: RoleService,
        private branchService: BranchService,
        private messageService: MessageService
    ) {
        this.userForm = this.fb.group({
            userName: ['', [Validators.required]],
            name: ['', [Validators.required]],
            surname: ['', [Validators.required]],
            emailAddress: ['', [Validators.required, Validators.email]],
            password: [''],
            isActive: [true],
            branchId: [null],
            roleNames: [[]],
            profilePictureUrl: [''],
        });
    }

    get dialogTitle(): string {
        return this.isEdit ? 'Edit User' : 'Create User';
    }

    get isEdit(): boolean {
        return !!this.userId;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadRoles();
            this.loadBranches();
            this.loadAllPermissions();
            if (this.userId) {
                this.loadUser(this.userId);
            } else {
                this.userForm
                    .get('password')
                    ?.setValidators([
                        Validators.required,
                        Validators.minLength(6),
                    ]);
                this.userForm.get('password')?.updateValueAndValidity();
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

    onRolesChange(): void {
        this.syncPermissionsFromSelectedRoles();
    }

    onSubmit(): void {
        if (this.userForm.invalid) {
            this.userForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        const formValue: any = { ...this.userForm.value };
        formValue.roleNames = this.selectedRoles;
        delete formValue.profilePictureUrl;

        if (
            this.profilePictureChanged &&
            this.profilePictureUrl &&
            this.profilePictureUrl.startsWith('data:image')
        ) {
            formValue.imageBase64 = this.profilePictureUrl;
        }

        if (this.isEdit && this.userId) {
            formValue.id = this.userId;
            if (!formValue.password || formValue.password.trim() === '') {
                delete formValue.password;
            }

            this.userService
                .update(formValue)
                .then(() =>
                    this.userService.updateUserPermissions({
                        id: this.userId!,
                        grantedPermissionNames:
                            this.selectedUserPermissions || [],
                    })
                )
                .then(() => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'User updated successfully',
                    });
                    this.saved.emit();
                    this.onHide();
                })
                .catch((error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.message || 'Failed to update user',
                    });
                })
                .finally(() => {
                    this.saving = false;
                });
        } else {
            this.userService
                .create(formValue)
                .then((createdUser) => {
                    if (!createdUser?.id) {
                        return Promise.resolve();
                    }
                    // Always sync permissions after create so role grants are not
                    // left in a prohibited state from stale user permission settings.
                    return this.userService.updateUserPermissions({
                        id: createdUser.id,
                        grantedPermissionNames:
                            this.selectedUserPermissions || [],
                    });
                })
                .then(() => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'User created successfully',
                    });
                    this.saved.emit();
                    this.onHide();
                })
                .catch((error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.message || 'Failed to create user',
                    });
                })
                .finally(() => {
                    this.saving = false;
                });
        }
    }

    onProfilePictureClick(): void {
        this.fileInput?.nativeElement?.click();
    }

    onFileSelect(event: any): void {
        const file =
            event.target.files && event.target.files.length > 0
                ? event.target.files[0]
                : null;
        if (!file) {
            return;
        }

        if (!file.type.match(/image\/(jpeg|jpg|png|gif|webp)/)) {
            this.messageService.add({
                severity: 'error',
                summary: 'Invalid File',
                detail: 'Please select a valid image file (JPEG, PNG, GIF, or WebP)',
            });
            if (this.fileInput) {
                this.fileInput.nativeElement.value = '';
            }
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            this.messageService.add({
                severity: 'error',
                summary: 'File Too Large',
                detail: 'Profile picture must be less than 5MB',
            });
            if (this.fileInput) {
                this.fileInput.nativeElement.value = '';
            }
            return;
        }

        const reader = new FileReader();
        reader.onload = (e: any) => {
            this.profilePictureUrl = e.target.result;
            this.profilePictureChanged = true;
        };
        reader.readAsDataURL(file);
    }

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
    }

    private resetForm(): void {
        this.userForm.reset({
            userName: '',
            name: '',
            surname: '',
            emailAddress: '',
            password: '',
            isActive: true,
            branchId: null,
            roleNames: [],
            profilePictureUrl: '',
        });
        this.userForm.get('password')?.clearValidators();
        this.userForm.get('password')?.updateValueAndValidity();
        this.selectedRoles = [];
        this.selectedUserPermissions = [];
        this.profilePictureUrl = null;
        this.profilePictureChanged = false;
        this.loading = false;
        this.saving = false;
        this.loadingPermissions = false;
        if (this.fileInput) {
            this.fileInput.nativeElement.value = '';
        }
    }

    private loadRoles(): void {
        this.roleService
            .getAll({ maxResultCount: 1000 })
            .then((result) => {
                this.roles = result.items || [];
            })
            .catch(() => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load roles',
                });
            });
    }

    private loadBranches(): void {
        this.branchService
            .getAll({ maxResultCount: 1000 })
            .then((result) => {
                this.branches = (result.items || []).filter((b) => b.isActive);
            })
            .catch(() => {
                this.branchService
                    .getLookup()
                    .then((items) => {
                        this.branches = items || [];
                    })
                    .catch(() => {
                        this.branches = [];
                    });
            });
    }

    private syncPermissionsFromSelectedRoles(): void {
        const roleNames = this.selectedRoles || [];
        if (!roleNames.length) {
            return;
        }

        const roleIds = this.roles
            .filter((role) => roleNames.includes(role.name))
            .map((role) => role.id)
            .filter((id) => !!id);

        if (!roleIds.length) {
            return;
        }

        Promise.all(
            roleIds.map((id) => this.roleService.getRoleForEdit(id))
        )
            .then((results) => {
                const fromRoles = new Set<string>(
                    this.selectedUserPermissions || []
                );
                results.forEach((result) => {
                    (result.grantedPermissionNames || []).forEach((name) => {
                        if (name) {
                            fromRoles.add(name);
                        }
                    });
                });
                this.selectedUserPermissions = Array.from(fromRoles);
            })
            .catch((error) => {
                console.error('Failed to sync role permissions:', error);
            });
    }

    private loadAllPermissions(): void {
        this.loadingPermissions = true;
        this.roleService
            .getAllPermissions()
            .then((permissions: PermissionDto[]) => {
                this.permissions = permissions || [];
                this.groupPermissions();
            })
            .catch((error: any) => {
                console.error('Failed to load permissions:', error);
            })
            .finally(() => {
                this.loadingPermissions = false;
            });
    }

    private loadUser(id: number): void {
        this.loading = true;
        this.roleService
            .getAll({ maxResultCount: 1000 })
            .then((result) => {
                this.roles = result.items || [];
                return this.userService.get(id);
            })
            .then((user) => {
                this.userForm.patchValue({
                    userName: user.userName,
                    name: user.name,
                    surname: user.surname,
                    emailAddress: user.emailAddress,
                    isActive: user.isActive,
                    branchId: user.branchId ?? null,
                    roleNames: user.roleNames || [],
                    profilePictureUrl: user.profilePictureUrl || '',
                    password: '',
                });

                if (user.profilePictureUrl) {
                    if (
                        user.profilePictureUrl.startsWith('http://') ||
                        user.profilePictureUrl.startsWith('https://')
                    ) {
                        this.profilePictureUrl = user.profilePictureUrl;
                    } else {
                        this.profilePictureUrl = `${environment.apiUrl}${
                            user.profilePictureUrl.startsWith('/') ? '' : '/'
                        }${user.profilePictureUrl}`;
                    }
                } else {
                    this.profilePictureUrl = null;
                }
                this.profilePictureChanged = false;

                if (
                    user.roleNames?.length &&
                    this.roles.length > 0
                ) {
                    this.selectedRoles = user.roleNames
                        .map((roleName: string) => {
                            if (!roleName) {
                                return null;
                            }
                            const role = this.roles.find(
                                (r) =>
                                    (r.normalizedName &&
                                        r.normalizedName.toLowerCase() ===
                                            roleName.toLowerCase()) ||
                                    (r.name &&
                                        r.name.toLowerCase() ===
                                            roleName.toLowerCase())
                            );
                            return role ? role.name : null;
                        })
                        .filter(
                            (r: string | null) => r != null
                        ) as string[];
                } else {
                    this.selectedRoles = [];
                }

                return this.userService.getUserPermissions(id);
            })
            .then((grantedPermissions) => {
                this.selectedUserPermissions = grantedPermissions || [];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load user',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private groupPermissions(): void {
        this.permissionGroups = {};
        if (!this.permissions?.length) {
            return;
        }
        this.permissions.forEach((permission: PermissionDto) => {
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

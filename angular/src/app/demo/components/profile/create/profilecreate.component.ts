import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { UserService } from 'src/app/demo/service/user.service';
import { RoleService } from 'src/app/demo/service/role.service';
import { RoleDto, UserDto } from 'src/app/demo/api/user-management';
import { PermissionDto } from 'src/app/demo/api/role-management';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

@Component({
    templateUrl: './profilecreate.component.html',
    providers: [MessageService]
})
export class ProfileCreateComponent implements OnInit { 

    userForm: FormGroup;
    roles: RoleDto[] = [];
    loading = false;
    selectedRoles: string[] = [];
    isEditMode = false;
    userId: number | null = null;
    profilePictureUrl: string | null = null;
    /** True when user picked a new image this session (data URL ready for ImageBase64). */
    profilePictureChanged = false;
    permissions: PermissionDto[] = [];
    loadingPermissions = false;
    selectedUserPermissions: string[] = [];
    permissionGroups: { [key: string]: PermissionDto[] } = {};
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

    constructor(
        private fb: FormBuilder,
        private userService: UserService,
        private roleService: RoleService,
        private router: Router,
        private route: ActivatedRoute,
        private messageService: MessageService
    ) {
        this.userForm = this.fb.group({
            userName: ['', [Validators.required]],
            name: ['', [Validators.required]],
            surname: ['', [Validators.required]],
            emailAddress: ['', [Validators.required, Validators.email]],
            password: [''],
            isActive: [true],
            roleNames: [[]],
            profilePictureUrl: ['']
        });
    }

    ngOnInit() {
        this.loadRoles();
        this.loadAllPermissions();
        
        // Check if we're in edit mode
        this.route.params.subscribe(params => {
            if (params['id']) {
                this.isEditMode = true;
                this.userId = +params['id'];
                this.loadUser(this.userId);
            } else {
                // In create mode, password is required
                this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
                this.userForm.get('password')?.updateValueAndValidity();
            }
        });
    }

    loadAllPermissions() {
        this.loadingPermissions = true;
        this.roleService.getAllPermissions()
            .then((permissions: PermissionDto[]) => {
                this.permissions = permissions || [];
                this.groupPermissions();
                this.loadingPermissions = false;
            })
            .catch((error: any) => {
                this.loadingPermissions = false;
                console.error('Failed to load permissions:', error);
            });
    }

    loadUser(id: number) {
        this.loading = true;
        // First ensure roles are loaded, then load user
        this.userService.getRoles()
            .then((roles) => {
                this.roles = roles;
                // Now load the user
                return this.userService.get(id);
            })
            .then((user) => {
                this.userForm.patchValue({
                    userName: user.userName,
                    name: user.name,
                    surname: user.surname,
                    emailAddress: user.emailAddress,
                    isActive: user.isActive,
                    roleNames: user.roleNames || [],
                    profilePictureUrl: user.profilePictureUrl || ''
                });
                
                // Set profile picture URL for display (construct full URL if relative path)
                if (user.profilePictureUrl) {
                    // If it's already a full URL, use it; otherwise construct from base URL
                    if (user.profilePictureUrl.startsWith('http://') || user.profilePictureUrl.startsWith('https://')) {
                        this.profilePictureUrl = user.profilePictureUrl;
                    } else {
                        // It's a relative path, construct full URL
                        this.profilePictureUrl = `${environment.apiUrl}${user.profilePictureUrl.startsWith('/') ? '' : '/'}${user.profilePictureUrl}`;
                    }
                } else {
                    this.profilePictureUrl = null;
                }
                this.profilePictureChanged = false;
                
                // Map roleNames to match the role options
                // Backend returns normalized role names (e.g., "ADMIN"), but we need regular names for multiSelect
                if (user.roleNames && Array.isArray(user.roleNames) && user.roleNames.length > 0 && this.roles.length > 0) {
                    this.selectedRoles = user.roleNames
                        .map((roleName: string) => {
                            if (!roleName) return null;
                            // Try to find role by normalized name first, then by regular name
                            const role = this.roles.find(r => 
                                (r.normalizedName && r.normalizedName.toLowerCase() === roleName.toLowerCase()) ||
                                (r.name && r.name.toLowerCase() === roleName.toLowerCase())
                            );
                            // Return the role name (not normalized) for multiSelect
                            return role ? role.name : null;
                        })
                        .filter((r: string | null) => r !== null && r !== undefined) as string[];
                } else {
                    this.selectedRoles = [];
                }
                
                // Load user permissions
                this.loadUserPermissions(id);
                
                this.loading = false;
            })
            .catch((error) => {
                this.loading = false;
                const errorMessage = error?.message || 'Failed to load user';
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: errorMessage 
                });
                this.router.navigate(['profile/list']);
            });
    }

    loadUserPermissions(userId: number) {
        this.loadingPermissions = true;
        // Load user's granted permissions
        this.userService.getUserPermissions(userId)
            .then((grantedPermissions) => {
                this.selectedUserPermissions = grantedPermissions || [];
                this.loadingPermissions = false;
            })
            .catch((error) => {
                this.loadingPermissions = false;
                // Don't show error for permissions, just log it
                console.error('Failed to load user permissions:', error);
            });
    }

    groupPermissions() {
        this.permissionGroups = {};
        if (!this.permissions || this.permissions.length === 0) {
            return;
        }
        this.permissions.forEach((permission: PermissionDto) => {
            if (!permission || !permission.name) {
                return;
            }
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

    loadRoles() {
        this.userService.getRoles()
            .then((roles) => {
                this.roles = roles;
            })
            .catch((error) => {
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Error', 
                    detail: 'Failed to load roles' 
                });
            });
    }

    onSubmit() {
        if (this.userForm.invalid) {
            this.userForm.markAllAsTouched();
            return;
        }

        this.loading = true;
        const formValue: any = { ...this.userForm.value };
        formValue.roleNames = this.selectedRoles;
        delete formValue.profilePictureUrl;

        // Backend saves images via ImageBase64 (same pattern as products/company profiles).
        // Do not call /api/FileUpload — that endpoint does not exist.
        if (
            this.profilePictureChanged &&
            this.profilePictureUrl &&
            this.profilePictureUrl.startsWith('data:image')
        ) {
            formValue.imageBase64 = this.profilePictureUrl;
        }

        this.submitForm(formValue);
    }

    private submitForm(formValue: any): void {
        if (this.isEditMode && this.userId) {
            // Update existing user
            formValue.id = this.userId;
            // Remove password if not provided in edit mode
            if (!formValue.password || formValue.password.trim() === '') {
                delete formValue.password;
            }
            
            this.userService.update(formValue)
                .then(() => {
                    // Update user permissions if in edit mode
                    if (this.isEditMode && this.userId) {
                        return this.userService.updateUserPermissions({
                            id: this.userId,
                            grantedPermissionNames: this.selectedUserPermissions || []
                        });
                    }
                    return Promise.resolve();
                })
                .then(() => {
                    this.messageService.add({ 
                        severity: 'success', 
                        summary: 'Success', 
                        detail: 'User updated successfully' 
                    });
                    this.router.navigate(['profile/list']);
                })
                .catch((error) => {
                    this.loading = false;
                    const errorMessage = error?.message || 'Failed to update user';
                    this.messageService.add({ 
                        severity: 'error', 
                        summary: 'Error', 
                        detail: errorMessage 
                    });
                });
        } else {
            // Create new user
            this.userService.create(formValue)
                .then((createdUser) => {
                    // Set permissions for newly created user
                    if (createdUser && createdUser.id && this.selectedUserPermissions.length > 0) {
                        return this.userService.updateUserPermissions({
                            id: createdUser.id,
                            grantedPermissionNames: this.selectedUserPermissions || []
                        });
                    }
                    return Promise.resolve();
                })
                .then(() => {
                    this.messageService.add({ 
                        severity: 'success', 
                        summary: 'Success', 
                        detail: 'User created successfully' 
                    });
                    this.router.navigate(['profile/list']);
                })
                .catch((error) => {
                    this.loading = false;
                    const errorMessage = error?.message || 'Failed to create user';
                    this.messageService.add({ 
                        severity: 'error', 
                        summary: 'Error', 
                        detail: errorMessage 
                    });
                });
        }
    }

    onProfilePictureClick(): void {
        this.fileInput.nativeElement.click();
    }

    onFileSelect(event: any): void {
        const file = event.target.files && event.target.files.length > 0 ? event.target.files[0] : null;
        if (file) {
            // Validate file type
            if (!file.type.match(/image\/(jpeg|jpg|png|gif|webp)/)) {
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'Invalid File', 
                    detail: 'Please select a valid image file (JPEG, PNG, GIF, or WebP)' 
                });
                // Reset file input
                if (this.fileInput) {
                    this.fileInput.nativeElement.value = '';
                }
                return;
            }
            
            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                this.messageService.add({ 
                    severity: 'error', 
                    summary: 'File Too Large', 
                    detail: 'Profile picture must be less than 5MB' 
                });
                // Reset file input
                if (this.fileInput) {
                    this.fileInput.nativeElement.value = '';
                }
                return;
            }
            
            // Preview + keep data URL for ImageBase64 on save
            const reader = new FileReader();
            reader.onload = (e: any) => {
                this.profilePictureUrl = e.target.result;
                this.profilePictureChanged = true;
            };
            reader.readAsDataURL(file);
        }
    }

    onFileRemove(): void {
        this.profilePictureChanged = false;
        this.profilePictureUrl = null;
        this.userForm.patchValue({ profilePictureUrl: '' });
        if (this.fileInput) {
            this.fileInput.nativeElement.value = '';
        }
    }

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
    }


    onCancel() {
        this.router.navigate(['profile/list']);
    }
    
}
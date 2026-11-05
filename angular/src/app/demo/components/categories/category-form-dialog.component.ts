import {
    Component,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { CategoryDto } from 'src/app/demo/api/category';
import { DepartmentDto } from 'src/app/demo/api/department';
import { UnitDto } from 'src/app/demo/api/unit';
import { CategoryService } from 'src/app/demo/service/category.service';
import { DepartmentService } from 'src/app/demo/service/department.service';
import { UnitService } from 'src/app/demo/service/unit.service';
import {
    isEntityNameValid,
    validateEntityName,
} from 'src/app/shared/utils/name-validation';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-category-form-dialog',
    templateUrl: './category-form-dialog.component.html',
})
export class CategoryFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() categoryId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    category: CategoryDto = { id: 0, name: '', description: '', departmentId: null };
    departments: DepartmentDto[] = [];
    units: UnitDto[] = [];
    saving = false;
    loading = false;
    imagePreview = '';
    private imageCleared = false;

    @HostListener('window:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (!this.visible || this.saving || this.loading) {
            return;
        }

        if (
            (event.altKey && (event.key === 's' || event.key === 'S')) ||
            (event.ctrlKey && (event.key === 's' || event.key === 'S'))
        ) {
            event.preventDefault();
            this.save();
        }
    }

    constructor(
        private categoryService: CategoryService,
        private departmentService: DepartmentService,
        private unitService: UnitService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.categoryId ? 'Edit Category' : 'Create Category';
    }

    get canSave(): boolean {
        return isEntityNameValid(this.category?.name);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadDepartments();
            this.loadUnits();
            if (this.categoryId) {
                this.loadCategory(this.categoryId);
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

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) {
            return;
        }

        const file = input.files[0];
        if (file.size > 5 * 1024 * 1024) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Image too large',
                detail: 'Please choose an image under 5 MB.',
            });
            input.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = () => {
            const result = reader.result as string;
            if (!result?.startsWith('data:image')) {
                return;
            }
            this.imagePreview = result;
            this.category.imageBase64 = result;
            this.imageCleared = false;
        };
        reader.readAsDataURL(file);
        input.value = '';
    }

    clearImage(): void {
        this.imagePreview = '';
        this.category.imageBase64 = undefined;
        this.category.imagePath = undefined;
        this.imageCleared = true;
    }

    save(): void {
        if (this.saving) {
            return;
        }

        const name = (this.category.name || '').trim();
        const nameError = validateEntityName(name, 'Category name');
        if (nameError) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: nameError,
            });
            return;
        }

        this.saving = true;
        const imageBase64 = this.category.imageBase64?.startsWith('data:image')
            ? this.category.imageBase64
            : this.imageCleared
              ? ''
              : undefined;

        const request = this.categoryId
            ? this.categoryService.update({
                  id: this.categoryId,
                  branchId: this.category.branchId,
                  name,
                  departmentId: this.category.departmentId ? Number(this.category.departmentId) : null,
                  description: this.category.description?.trim() || undefined,
                  defaultUnitId: this.category.defaultUnitId || undefined,
                  imagePath: this.imageCleared
                      ? undefined
                      : this.category.imagePath,
                  imageBase64,
              })
            : this.categoryService.create({
                  name,
                  departmentId: this.category.departmentId ? Number(this.category.departmentId) : null,
                  description: this.category.description?.trim() || undefined,
                  defaultUnitId: this.category.defaultUnitId || undefined,
                  imageBase64,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.categoryId
                        ? 'Category updated successfully'
                        : 'Category created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save category',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private loadDepartments(): void {
        this.departmentService
            .getLookup(true)
            .then((departments) => {
                this.departments = departments || [];
            })
            .catch(() => {
                this.departments = [];
            });
    }

    private loadUnits(): void {
        this.unitService
            .getLookup(true)
            .then((units) => {
                this.units = units || [];
            })
            .catch(() => {
                this.units = [];
            });
    }

    private resetForm(): void {
        this.category = { id: 0, name: '', description: '', departmentId: null };
        this.imagePreview = '';
        this.imageCleared = false;
        this.saving = false;
        this.loading = false;
    }

    private loadCategory(id: number): void {
        this.loading = true;
        this.categoryService
            .get(id)
            .then((category) => {
                this.category = { ...category, departmentId: category.departmentId ?? null, imageBase64: undefined };
                this.imagePreview = this.categoryService.getImageUrl(
                    category.imagePath
                );
                this.imageCleared = false;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load category',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

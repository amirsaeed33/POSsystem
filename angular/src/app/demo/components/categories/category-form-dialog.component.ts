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
import { UnitDto } from 'src/app/demo/api/unit';
import { CategoryService } from 'src/app/demo/service/category.service';
import { UnitService } from 'src/app/demo/service/unit.service';
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

    category: CategoryDto = { id: 0, name: '', description: '' };
    units: UnitDto[] = [];
    saving = false;
    loading = false;

    @HostListener('window:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (!this.visible || this.saving || this.loading) {
            return;
        }

        // Alt + S or Ctrl + S to save without browser conflict
        if ((event.altKey && (event.key === 's' || event.key === 'S')) ||
            (event.ctrlKey && (event.key === 's' || event.key === 'S'))) {
            event.preventDefault();
            this.save();
        }
    }

    constructor(
        private categoryService: CategoryService,
        private unitService: UnitService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.categoryId ? 'Edit Category' : 'Create Category';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
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

    save(): void {
        const name = (this.category.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const request = this.categoryId
            ? this.categoryService.update({
                  id: this.categoryId,
                  name,
                  description: this.category.description?.trim() || undefined,
                  defaultUnitId: this.category.defaultUnitId || undefined,
              })
            : this.categoryService.create({
                  name,
                  description: this.category.description?.trim() || undefined,
                  defaultUnitId: this.category.defaultUnitId || undefined,
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

    private loadUnits(): void {
        this.unitService
            .getLookup()
            .then((units) => {
                this.units = units || [];
            })
            .catch(() => {
                this.units = [];
            });
    }

    private resetForm(): void {
        this.category = { id: 0, name: '', description: '' };
        this.saving = false;
        this.loading = false;
    }

    private loadCategory(id: number): void {
        this.loading = true;
        this.categoryService
            .get(id)
            .then((category) => {
                this.category = { ...category };
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

import {
    Component,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { BrandDto } from 'src/app/demo/api/brand';
import { BrandService } from 'src/app/demo/service/brand.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-brand-form-dialog',
    templateUrl: './brand-form-dialog.component.html',
})
export class BrandFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() brandId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    brand: BrandDto = { id: 0, name: '', description: '' };
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
        private brandService: BrandService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.brandId ? 'Edit Brand' : 'Create Brand';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.brandId) {
                this.loadBrand(this.brandId);
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
        const name = (this.brand.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const request = this.brandId
            ? this.brandService.update({
                  id: this.brandId,
                  name,
                  description: this.brand.description?.trim() || undefined,
              })
            : this.brandService.create({
                  name,
                  description: this.brand.description?.trim() || undefined,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.brandId
                        ? 'Brand updated successfully'
                        : 'Brand created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save brand',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.brand = { id: 0, name: '', description: '' };
        this.saving = false;
        this.loading = false;
    }

    private loadBrand(id: number): void {
        this.loading = true;
        this.brandService
            .get(id)
            .then((brand) => {
                this.brand = { ...brand };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load brand',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

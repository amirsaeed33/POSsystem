import {
    Component,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { UnitDto } from 'src/app/demo/api/unit';
import { UnitService } from 'src/app/demo/service/unit.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-unit-form-dialog',
    templateUrl: './unit-form-dialog.component.html',
})
export class UnitFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() unitId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    unit: UnitDto = { id: 0, name: '', description: '', symbol: '' };
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
        private unitService: UnitService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.unitId ? 'Edit Unit' : 'Create Unit';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.unitId) {
                this.loadUnit(this.unitId);
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
        const name = (this.unit.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const request = this.unitId
            ? this.unitService.update({
                  id: this.unitId,
                  name,
                  description: this.unit.description?.trim() || undefined,
              })
            : this.unitService.create({
                  name,
                  description: this.unit.description?.trim() || undefined,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.unitId
                        ? 'Unit updated successfully'
                        : 'Unit created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save unit',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.unit = { id: 0, name: '', description: '' };
        this.saving = false;
        this.loading = false;
    }

    private loadUnit(id: number): void {
        this.loading = true;
        this.unitService
            .get(id)
            .then((unit) => {
                this.unit = { ...unit };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load unit',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}

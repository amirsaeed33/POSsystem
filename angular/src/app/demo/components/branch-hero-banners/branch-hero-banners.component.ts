import {
    ChangeDetectorRef,
    Component,
    Input,
    OnChanges,
    OnInit,
    SimpleChanges,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    CreateOrUpdateOnlineOrderHeroSlideDto,
    OnlineOrderHeroSlideDto,
} from 'src/app/demo/api/online-order-hero-slide';
import { OnlineOrderHeroSlideService } from 'src/app/demo/service/online-order-hero-slide.service';

@Component({
    selector: 'app-branch-hero-banners',
    templateUrl: './branch-hero-banners.component.html',
    styleUrls: ['./branch-hero-banners.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class BranchHeroBannersComponent implements OnInit, OnChanges {
    @Input() branchId?: number;

    slides: OnlineOrderHeroSlideDto[] = [];
    loading = false;
    saving = false;
    private thumbVersion = Date.now();

    dialogVisible = false;
    editing: CreateOrUpdateOnlineOrderHeroSlideDto | null = null;
    imagePreview = '';
    bgImagePreview = '';

    targetOptions = [
        { label: 'Menu', value: 'menu' },
        { label: 'Deals', value: 'deals' },
        { label: 'Home', value: 'home' },
    ];

    constructor(
        private heroService: OnlineOrderHeroSlideService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private cd: ChangeDetectorRef
    ) {}

    ngOnInit(): void {
        if (this.branchId) {
            void this.load();
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['branchId'] && this.branchId) {
            void this.load();
        }
    }

    async load(): Promise<void> {
        if (!this.branchId) {
            this.slides = [];
            return;
        }
        this.loading = true;
        try {
            this.slides = await this.heroService.getByBranch(this.branchId);
            this.thumbVersion = Date.now();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load hero banners',
            });
            this.slides = [];
        } finally {
            this.loading = false;
        }
    }

    openCreate(): void {
        if (!this.branchId) {
            return;
        }
        this.editing = {
            id: null,
            branchId: this.branchId,
            sortOrder: (this.slides?.length || 0) + 1,
            isActive: true,
            badge: '',
            badgeIcon: '🔥',
            titleMain: '',
            titleHighlight: '',
            titleEnd: '',
            description: '',
            offerPill: '',
            rating: '4.9★',
            primaryBtnText: 'Order Now',
            primaryTarget: 'deals',
            secondaryBtnText: 'Explore Full Menu',
            secondaryTarget: 'menu',
            imagePath: '',
            bgImagePath: '',
        };
        this.imagePreview = '';
        this.bgImagePreview = '';
        this.dialogVisible = true;
    }

    openEdit(slide: OnlineOrderHeroSlideDto): void {
        this.editing = {
            id: slide.id,
            branchId: slide.branchId,
            sortOrder: Number(slide.sortOrder) || 0,
            isActive: slide.isActive,
            badge: slide.badge || '',
            badgeIcon: slide.badgeIcon || '',
            titleMain: slide.titleMain || '',
            titleHighlight: slide.titleHighlight || '',
            titleEnd: slide.titleEnd || '',
            description: slide.description || '',
            offerPill: slide.offerPill || '',
            rating: slide.rating || '',
            primaryBtnText: slide.primaryBtnText || 'Order Now',
            primaryTarget: slide.primaryTarget || 'menu',
            secondaryBtnText: slide.secondaryBtnText || 'Explore Full Menu',
            secondaryTarget: slide.secondaryTarget || 'menu',
            imagePath: slide.imagePath || '',
            bgImagePath: slide.bgImagePath || '',
            imageBase64: undefined,
            bgImageBase64: undefined,
        };
        this.imagePreview = this.heroService.getImageUrl(slide.imagePath);
        this.bgImagePreview = this.heroService.getImageUrl(
            slide.bgImagePath || slide.imagePath
        );
        this.dialogVisible = true;
    }

    bumpSort(delta: number): void {
        if (!this.editing) {
            return;
        }
        const next = Math.max(0, (Number(this.editing.sortOrder) || 0) + delta);
        this.editing.sortOrder = next;
    }

    onImageSelected(event: Event, kind: 'image' | 'bg'): void {
        const input = event.target as HTMLInputElement;
        const file = input?.files?.[0];
        if (!file || !this.editing) {
            return;
        }
        if (!file.type.startsWith('image/')) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Invalid file',
                detail: 'Please select an image file.',
            });
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            this.messageService.add({
                severity: 'warn',
                summary: 'File too large',
                detail: 'Please upload an image under 5 MB.',
            });
            return;
        }
        const reader = new FileReader();
        reader.onload = () => {
            const result = String(reader.result || '');
            if (!this.editing || !result) {
                return;
            }
            if (kind === 'image') {
                this.editing.imageBase64 = result;
                this.editing.imagePath = '';
                this.imagePreview = result;
                // Card upload replaces storefront bg unless a separate bg was chosen in this edit.
                if (!this.editing.bgImageBase64) {
                    this.editing.bgImagePath = '';
                    this.bgImagePreview = result;
                }
            } else {
                this.editing.bgImageBase64 = result;
                this.editing.bgImagePath = '';
                this.bgImagePreview = result;
            }
            this.cd.detectChanges();
        };
        reader.onerror = () => {
            this.messageService.add({
                severity: 'error',
                summary: 'Image',
                detail: 'Could not read the selected image file.',
            });
        };
        reader.readAsDataURL(file);
        input.value = '';
    }

    clearImage(kind: 'image' | 'bg'): void {
        if (!this.editing) {
            return;
        }
        if (kind === 'image') {
            this.editing.imageBase64 = undefined;
            this.editing.imagePath = '';
            this.imagePreview = '';
        } else {
            this.editing.bgImageBase64 = undefined;
            this.editing.bgImagePath = '';
            this.bgImagePreview = '';
        }
    }

    async save(): Promise<void> {
        if (!this.editing || !this.branchId) {
            return;
        }
        if (!(this.editing.titleMain || '').trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Title is required.',
            });
            return;
        }

        this.saving = true;
        try {
            const hasNewCard = !!this.editing.imageBase64;
            const hasNewBg = !!this.editing.bgImageBase64;
            const payload: CreateOrUpdateOnlineOrderHeroSlideDto = {
                ...this.editing,
                branchId: this.branchId,
                sortOrder: Number(this.editing.sortOrder) || 0,
                titleMain: this.editing.titleMain.trim(),
                imageBase64: this.editing.imageBase64 || undefined,
                bgImageBase64: this.editing.bgImageBase64 || undefined,
                // Don't keep stale remote/local paths when a new file is uploaded.
                imagePath: hasNewCard ? '' : this.editing.imagePath || undefined,
                bgImagePath: hasNewBg || hasNewCard ? '' : this.editing.bgImagePath || undefined,
            };
            await this.heroService.createOrUpdate(payload);
            this.messageService.add({
                severity: 'success',
                summary: 'Saved',
                detail: 'Hero banner saved.',
            });
            this.dialogVisible = false;
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to save hero banner',
            });
        } finally {
            this.saving = false;
        }
    }

    confirmDelete(slide: OnlineOrderHeroSlideDto): void {
        this.confirmationService.confirm({
            message: `Delete banner "${slide.titleMain}"?`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => void this.delete(slide.id),
        });
    }

    async delete(id: number): Promise<void> {
        try {
            await this.heroService.delete(id);
            this.messageService.add({
                severity: 'success',
                summary: 'Deleted',
                detail: 'Hero banner removed.',
            });
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to delete hero banner',
            });
        }
    }

    getThumb(path?: string): string {
        const url = this.heroService.getImageUrl(path);
        if (!url || url.startsWith('data:') || url.startsWith('http')) {
            return url;
        }
        // Cache-bust relative API paths after save/reload (stable within a load).
        return `${url}${url.includes('?') ? '&' : '?'}v=${this.thumbVersion}`;
    }

    onPreviewError(kind: 'image' | 'bg'): void {
        if (kind === 'image') {
            this.imagePreview = '';
        } else {
            this.bgImagePreview = '';
        }
    }
}

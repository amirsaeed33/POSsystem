import {
    Component,
    Input,
    Output,
    EventEmitter,
    ChangeDetectionStrategy,
    ViewEncapsulation,
    OnChanges,
    SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { OverlayModule, ConnectedPosition } from '@angular/cdk/overlay';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';

export type RowActionGroup = 'view-edit' | 'status' | 'danger';
export type RowActionColor = 'blue' | 'green' | 'amber' | 'red' | 'indigo' | 'gray';

export interface RowActionItem {
    id?: string;
    label: string;
    icon?: string;
    action?: (event?: MouseEvent) => void;
    command?: (event?: MouseEvent) => void;
    visible?: boolean;
    disabled?: boolean;
    group?: RowActionGroup;
    color?: RowActionColor;
    danger?: boolean;
}

interface ProcessedActionItem {
    id?: string;
    label: string;
    icon?: string;
    action?: (event?: MouseEvent) => void;
    command?: (event?: MouseEvent) => void;
    disabled?: boolean;
    group: RowActionGroup;
    color: RowActionColor;
    danger: boolean;
}

interface ActionGroup {
    key: RowActionGroup;
    items: ProcessedActionItem[];
}

@Component({
    selector: 'app-row-actions',
    standalone: true,
    imports: [CommonModule, OverlayModule, TooltipModule, RippleModule],
    template: `
        <div class="row-actions-container">
            <button
                type="button"
                class="row-actions-trigger"
                [class.is-open]="isOpen"
                cdkOverlayOrigin
                #trigger="cdkOverlayOrigin"
                (click)="toggleMenu($event)"
                [pTooltip]="tooltip"
                tooltipPosition="left"
                [attr.aria-expanded]="isOpen"
                aria-haspopup="true"
            >
                <i [class]="icon"></i>
            </button>

            <ng-template
                cdkConnectedOverlay
                [cdkConnectedOverlayOrigin]="trigger"
                [cdkConnectedOverlayOpen]="isOpen"
                [cdkConnectedOverlayHasBackdrop]="true"
                cdkConnectedOverlayBackdropClass="cdk-overlay-transparent-backdrop"
                [cdkConnectedOverlayPositions]="overlayPositions"
                (backdropClick)="closeMenu()"
                (detach)="closeMenu()"
            >
                <div class="row-actions-menu" (click)="$event.stopPropagation()">
                    <ng-container *ngFor="let group of activeGroups; let lastGroup = last">
                        <div class="row-actions-group">
                            <a
                                *ngFor="let item of group.items"
                                class="row-actions-item"
                                [class.is-danger]="item.danger"
                                [class.is-disabled]="item.disabled"
                                (click)="onItemClick(item, $event)"
                                pRipple
                            >
                                <span class="row-actions-icon-box" [ngClass]="'color-' + item.color">
                                    <i [class]="item.icon"></i>
                                </span>
                                <span class="row-actions-label">{{ item.label }}</span>
                            </a>
                        </div>
                        <div *ngIf="!lastGroup" class="row-actions-separator"></div>
                    </ng-container>
                </div>
            </ng-template>
        </div>
    `,
    styles: [
        `
        .row-actions-container {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            position: relative;
        }

        .row-actions-trigger {
            width: 2rem;
            height: 2rem;
            border-radius: 50%;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            background: transparent;
            border: none;
            cursor: pointer;
            color: var(--text-color, #1e293b);
            transition: background-color 0.15s ease, color 0.15s ease;
            padding: 0;

            &:hover,
            &.is-open {
                background-color: var(--surface-hover, #f1f5f9);
            }

            &:focus-visible {
                outline: 2px solid var(--primary-color, #4f46e5);
                outline-offset: 1px;
            }

            i {
                font-size: 1rem;
            }
        }

        .row-actions-menu {
            min-width: 200px;
            max-width: 220px;
            width: max-content;
            background-color: var(--surface-overlay, #ffffff);
            border: 0.5px solid var(--surface-border, #e2e8f0);
            border-radius: 12px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.12), 0 8px 10px -6px rgba(0, 0, 0, 0.08);
            padding: 6px 0;
            box-sizing: border-box;
            overflow: hidden;
            user-select: none;
            animation: rowActionFadeIn 0.12s cubic-bezier(0.16, 1, 0.3, 1);
            transform-origin: top right;
        }

        @keyframes rowActionFadeIn {
            from {
                opacity: 0;
                transform: scale(0.96);
            }
            to {
                opacity: 1;
                transform: scale(1);
            }
        }

        .row-actions-group {
            display: flex;
            flex-direction: column;
        }

        .row-actions-separator {
            height: 0;
            border: none;
            border-top: 0.5px solid var(--surface-border, #e2e8f0);
            margin: 6px 0;
        }

        .row-actions-item {
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 10px 14px;
            margin: 2px 6px;
            border-radius: 8px;
            font-size: 14px;
            line-height: 1.4;
            font-weight: 500;
            min-height: 44px;
            box-sizing: border-box;
            color: var(--text-color, #1e293b);
            cursor: pointer;
            transition: background-color 0.15s ease, color 0.15s ease;
            text-decoration: none;

            &:hover {
                background-color: var(--surface-hover, #f1f5f9);
            }

            &.is-danger {
                color: #dc2626 !important;

                &:hover {
                    background-color: rgba(239, 68, 68, 0.08) !important;
                }
            }

            &.is-disabled {
                opacity: 0.45;
                pointer-events: none;
                cursor: not-allowed;
            }
        }

        .row-actions-label {
            flex: 1;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .row-actions-icon-box {
            width: 28px;
            height: 28px;
            min-width: 28px;
            min-height: 28px;
            border-radius: 8px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            font-size: 14px;
            transition: transform 0.15s ease;

            &.color-blue {
                background-color: rgba(59, 130, 246, 0.12);
                color: #2563eb;
            }

            &.color-green {
                background-color: rgba(34, 197, 94, 0.14);
                color: #16a34a;
            }

            &.color-amber {
                background-color: rgba(245, 158, 11, 0.15);
                color: #d97706;
            }

            &.color-red {
                background-color: rgba(239, 68, 68, 0.12);
                color: #dc2626;
            }

            &.color-indigo {
                background-color: rgba(99, 102, 241, 0.12);
                color: #4f46e5;
            }

            &.color-gray {
                background-color: rgba(100, 116, 139, 0.12);
                color: #64748b;
            }
        }
        `,
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppRowActionsComponent implements OnChanges {
    @Input() items: RowActionItem[] = [];
    @Input() tooltip = 'Actions';
    @Input() icon = 'pi pi-ellipsis-v';

    @Output() menuOpened = new EventEmitter<void>();
    @Output() menuClosed = new EventEmitter<void>();

    isOpen = false;
    activeGroups: ActionGroup[] = [];

    readonly overlayPositions: ConnectedPosition[] = [
        {
            originX: 'end',
            originY: 'bottom',
            overlayX: 'end',
            overlayY: 'top',
            offsetY: 4,
        },
        {
            originX: 'end',
            originY: 'top',
            overlayX: 'end',
            overlayY: 'bottom',
            offsetY: -4,
        },
    ];

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['items']) {
            this.rebuildGroups();
        }
    }

    toggleMenu(event: MouseEvent): void {
        event.stopPropagation();
        event.preventDefault();
        this.isOpen = !this.isOpen;
        if (this.isOpen) {
            this.rebuildGroups();
            this.menuOpened.emit();
        } else {
            this.menuClosed.emit();
        }
    }

    closeMenu(): void {
        if (this.isOpen) {
            this.isOpen = false;
            this.menuClosed.emit();
        }
    }

    onItemClick(item: ProcessedActionItem, event: MouseEvent): void {
        if (item.disabled) return;
        event.stopPropagation();
        this.closeMenu();
        if (item.action) {
            item.action(event);
        } else if (item.command) {
            item.command(event);
        }
    }

    private rebuildGroups(): void {
        const rawItems = Array.isArray(this.items) ? this.items : [];
        const visibleItems = rawItems.filter((i) => i && i.visible !== false);

        const viewEditItems: ProcessedActionItem[] = [];
        const statusItems: ProcessedActionItem[] = [];
        const dangerItems: ProcessedActionItem[] = [];

        for (const item of visibleItems) {
            const group = this.resolveGroup(item);
            const color = this.resolveColor(item);
            const danger = group === 'danger' || !!item.danger;
            const label = this.toSentenceCase(item.label);

            const processed: ProcessedActionItem = {
                id: item.id,
                label,
                icon: item.icon,
                action: item.action,
                command: item.command,
                disabled: item.disabled,
                group,
                color,
                danger,
            };

            if (group === 'danger') {
                dangerItems.push(processed);
            } else if (group === 'status') {
                statusItems.push(processed);
            } else {
                viewEditItems.push(processed);
            }
        }

        const groups: ActionGroup[] = [];
        if (viewEditItems.length > 0) {
            groups.push({ key: 'view-edit', items: viewEditItems });
        }
        if (statusItems.length > 0) {
            groups.push({ key: 'status', items: statusItems });
        }
        if (dangerItems.length > 0) {
            groups.push({ key: 'danger', items: dangerItems });
        }

        this.activeGroups = groups;
    }

    private resolveGroup(item: RowActionItem): RowActionGroup {
        if (item.group) {
            return item.group;
        }
        if (item.danger) {
            return 'danger';
        }
        const labelLower = (item.label || '').toLowerCase();
        const iconLower = (item.icon || '').toLowerCase();

        if (
            labelLower.includes('delete') ||
            labelLower.includes('remove') ||
            labelLower.includes('trash') ||
            iconLower.includes('trash')
        ) {
            return 'danger';
        }

        if (
            labelLower.includes('approve') ||
            labelLower.includes('reject') ||
            labelLower.includes('pay') ||
            labelLower.includes('salary') ||
            labelLower.includes('activate') ||
            labelLower.includes('deactivate') ||
            labelLower.includes('login') ||
            labelLower.includes('password') ||
            labelLower.includes('status') ||
            labelLower.includes('return') ||
            iconLower.includes('check') ||
            iconLower.includes('times') ||
            iconLower.includes('dollar') ||
            iconLower.includes('wallet') ||
            iconLower.includes('ban') ||
            iconLower.includes('key') ||
            iconLower.includes('replay')
        ) {
            return 'status';
        }

        return 'view-edit';
    }

    private resolveColor(item: RowActionItem): RowActionColor {
        if (item.color) {
            return item.color;
        }
        if (item.danger) {
            return 'red';
        }
        const labelLower = (item.label || '').toLowerCase();
        const iconLower = (item.icon || '').toLowerCase();

        if (labelLower.includes('delete') || iconLower.includes('trash')) {
            return 'red';
        }
        if (
            labelLower.includes('approve') ||
            labelLower === 'activate' ||
            iconLower.includes('check')
        ) {
            return 'green';
        }
        if (
            labelLower.includes('reject') ||
            labelLower === 'deactivate' ||
            iconLower.includes('ban') ||
            iconLower.includes('times')
        ) {
            return 'amber';
        }
        if (
            labelLower.includes('pay') ||
            labelLower.includes('salary') ||
            iconLower.includes('dollar') ||
            iconLower.includes('wallet')
        ) {
            return 'green';
        }
        if (
            labelLower.includes('return') ||
            iconLower.includes('replay')
        ) {
            return 'amber';
        }
        if (
            labelLower.includes('key') ||
            labelLower.includes('password') ||
            labelLower.includes('login')
        ) {
            return 'indigo';
        }
        if (labelLower.includes('print') || iconLower.includes('print')) {
            return 'indigo';
        }
        if (
            labelLower.includes('view') ||
            labelLower.includes('preview') ||
            iconLower.includes('eye')
        ) {
            return 'blue';
        }
        if (labelLower.includes('edit') || iconLower.includes('pencil')) {
            return 'blue';
        }
        return 'blue';
    }

    private toSentenceCase(text: string): string {
        if (!text) return '';
        const trimmed = text.trim();
        const words = trimmed.split(/\s+/);
        if (words.length === 0) return '';
        if (words.length === 1) {
            return words[0].charAt(0).toUpperCase() + words[0].slice(1).toLowerCase();
        }
        return words
            .map((w, index) => {
                if (index === 0) {
                    return w.charAt(0).toUpperCase() + w.slice(1).toLowerCase();
                }
                return w.toLowerCase();
            })
            .join(' ');
    }
}

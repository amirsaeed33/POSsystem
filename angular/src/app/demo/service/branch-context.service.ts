import { Injectable, Injector } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { BranchDto, BranchStatuses } from '../api/branch';
import { BranchService } from './branch.service';
import { AppReadCacheService } from './app-read-cache.service';

const BRANCH_STORAGE_KEY = 'SmartPos.BranchId';
export const HOST_ADMIN_STORAGE_KEY = 'SmartPos.IsHostAdmin';

@Injectable({ providedIn: 'root' })
export class BranchContextService {
    private readonly currentBranchSubject = new BehaviorSubject<BranchDto | null>(
        null
    );
    private allowedBranches: BranchDto[] = [];
    private loadPromise: Promise<BranchDto[]> | null = null;

    readonly currentBranch$: Observable<BranchDto | null> =
        this.currentBranchSubject.asObservable();

    constructor(
        private branchService: BranchService,
        private injector: Injector
    ) {}

    getBranchId(): number | null {
        return this.toBranchId(this.currentBranchSubject.value?.id)
            ?? this.readStoredBranchId();
    }

    getCurrentBranch(): BranchDto | null {
        return this.currentBranchSubject.value;
    }

    getAllowedBranches(): BranchDto[] {
        return this.allowedBranches;
    }

    isRestaurant(): boolean {
        const branch = this.currentBranchSubject.value;
        if (!branch) {
            return false;
        }
        return Number(branch.businessType) === 2;
    }

    /** True when the selected branch is Approved (or none selected). */
    isCurrentBranchApproved(): boolean {
        const branch = this.currentBranchSubject.value;
        if (!branch) {
            return true;
        }
        return (branch.status || '').toLowerCase() === BranchStatuses.Approved.toLowerCase();
    }

    isCurrentBranchPendingApproval(): boolean {
        const branch = this.currentBranchSubject.value;
        if (!branch) {
            return false;
        }
        const status = (branch.status || BranchStatuses.Pending).toLowerCase();
        return status !== BranchStatuses.Approved.toLowerCase();
    }

    setCurrentBranch(branch: BranchDto | null): void {
        if (!branch?.id) {
            this.clear();
            return;
        }

        const previousId = this.toBranchId(this.currentBranchSubject.value?.id);
        const nextId = this.toBranchId(branch.id);
        localStorage.setItem(BRANCH_STORAGE_KEY, String(branch.id));
        this.currentBranchSubject.next(branch);

        if (previousId !== nextId) {
            this.clearBranchScopedCaches();
        }
    }

    clear(): void {
        localStorage.removeItem(BRANCH_STORAGE_KEY);
        this.currentBranchSubject.next(null);
        this.allowedBranches = [];
        this.clearBranchScopedCaches();
    }

    private clearBranchScopedCaches(): void {
        try {
            this.injector.get(AppReadCacheService).clearBranchScoped();
        } catch {
            // AppReadCacheService may not be available during early bootstrap.
        }
    }

    static setHostAdminSession(isHostAdmin: boolean): void {
        if (isHostAdmin) {
            localStorage.setItem(HOST_ADMIN_STORAGE_KEY, 'true');
        } else {
            localStorage.removeItem(HOST_ADMIN_STORAGE_KEY);
        }
    }

    static isHostAdminSession(): boolean {
        return localStorage.getItem(HOST_ADMIN_STORAGE_KEY) === 'true';
    }

    async ensureLoaded(preferredBranchId?: number | null): Promise<BranchDto[]> {
        if (this.loadPromise) {
            return this.loadPromise;
        }

        this.loadPromise = this.loadBranches(preferredBranchId).finally(() => {
            this.loadPromise = null;
        });
        return this.loadPromise;
    }

    private async loadBranches(
        preferredBranchId?: number | null
    ): Promise<BranchDto[]> {
        const branches = await this.branchService.getLookup();
        this.allowedBranches = branches;

        if (!branches.length) {
            this.currentBranchSubject.next(null);
            return branches;
        }

        const storedId = this.readStoredBranchId();
        const preferred = this.toBranchId(preferredBranchId);
        const currentId = this.toBranchId(this.currentBranchSubject.value?.id);

        // Priority: in-memory selection → persisted user choice → login preferred → first
        // Never let preferred/user.branchId override a valid stored selection after refresh.
        const selected =
            this.findBranch(branches, currentId) ||
            this.findBranch(branches, storedId) ||
            this.findBranch(branches, preferred) ||
            branches[0];

        this.setCurrentBranch(selected);
        return branches;
    }

    private findBranch(
        branches: BranchDto[],
        id: number | null
    ): BranchDto | undefined {
        if (id == null) {
            return undefined;
        }
        return branches.find((b) => this.toBranchId(b.id) === id);
    }

    private readStoredBranchId(): number | null {
        return this.toBranchId(localStorage.getItem(BRANCH_STORAGE_KEY));
    }

    private toBranchId(value: unknown): number | null {
        if (value == null || value === '') {
            return null;
        }
        const n = typeof value === 'number' ? value : parseInt(String(value), 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }
}

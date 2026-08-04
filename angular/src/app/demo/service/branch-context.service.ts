import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { BranchDto } from '../api/branch';
import { BranchService } from './branch.service';

const BRANCH_STORAGE_KEY = 'SmartPos.BranchId';

@Injectable({ providedIn: 'root' })
export class BranchContextService {
    private readonly currentBranchSubject = new BehaviorSubject<BranchDto | null>(
        null
    );
    private allowedBranches: BranchDto[] = [];
    private loadPromise: Promise<BranchDto[]> | null = null;

    readonly currentBranch$: Observable<BranchDto | null> =
        this.currentBranchSubject.asObservable();

    constructor(private branchService: BranchService) {}

    getBranchId(): number | null {
        return this.currentBranchSubject.value?.id ?? null;
    }

    getCurrentBranch(): BranchDto | null {
        return this.currentBranchSubject.value;
    }

    getAllowedBranches(): BranchDto[] {
        return this.allowedBranches;
    }

    setCurrentBranch(branch: BranchDto | null): void {
        if (!branch?.id) {
            this.clear();
            return;
        }

        localStorage.setItem(BRANCH_STORAGE_KEY, String(branch.id));
        this.currentBranchSubject.next(branch);
    }

    clear(): void {
        localStorage.removeItem(BRANCH_STORAGE_KEY);
        this.currentBranchSubject.next(null);
        this.allowedBranches = [];
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

        const storedRaw = localStorage.getItem(BRANCH_STORAGE_KEY);
        const storedId = storedRaw ? parseInt(storedRaw, 10) : NaN;
        const preferred =
            preferredBranchId != null && Number.isFinite(preferredBranchId)
                ? preferredBranchId
                : null;
        const currentId = this.currentBranchSubject.value?.id ?? null;

        // Keep an already-selected / stored branch when still allowed so a
        // session reload (or HMR) does not silently reset the user's choice.
        const selected =
            branches.find((b) => b.id === currentId) ||
            branches.find((b) => b.id === storedId) ||
            branches.find((b) => b.id === preferred) ||
            branches.find((b) => b.isDefault) ||
            branches[0];

        if (this.currentBranchSubject.value?.id !== selected.id) {
            this.setCurrentBranch(selected);
        }
        return branches;
    }
}

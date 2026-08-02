import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { BranchDto } from '../api/branch';
import { BranchService } from './branch.service';

@Injectable({
    providedIn: 'root',
})
export class BranchContextService {
    private readonly branchesSubject = new BehaviorSubject<BranchDto[]>([]);
    private readonly currentBranchSubject = new BehaviorSubject<BranchDto | null>(
        null
    );
    private readonly changedSubject = new BehaviorSubject<number | null>(null);

    readonly branches$: Observable<BranchDto[]> =
        this.branchesSubject.asObservable();
    readonly currentBranch$: Observable<BranchDto | null> =
        this.currentBranchSubject.asObservable();
    readonly changed$: Observable<number | null> =
        this.changedSubject.asObservable();

    private loadPromise: Promise<BranchDto[]> | null = null;

    constructor(private branchService: BranchService) {}

    getBranchId(): number | null {
        return this.currentBranchSubject.value?.id ?? null;
    }

    getCurrentBranch(): BranchDto | null {
        return this.currentBranchSubject.value;
    }

    getBranches(): BranchDto[] {
        return this.branchesSubject.value;
    }

    setBranch(branch: BranchDto | null): void {
        const previousId = this.currentBranchSubject.value?.id ?? null;
        const nextId = branch?.id ?? null;
        this.currentBranchSubject.next(branch);
        if (previousId !== nextId) {
            this.changedSubject.next(nextId);
        }
    }

    setBranchById(branchId: number | null): void {
        if (branchId == null) {
            this.setBranch(null);
            return;
        }
        const branch = this.branchesSubject.value.find((b) => b.id === branchId);
        if (branch) {
            this.setBranch(branch);
        }
    }

    clear(): void {
        this.branchesSubject.next([]);
        this.currentBranchSubject.next(null);
        this.changedSubject.next(null);
        this.loadPromise = null;
    }

    async ensureLoaded(force = false): Promise<BranchDto[]> {
        if (!force && this.loadPromise) {
            return this.loadPromise;
        }

        this.loadPromise = this.branchService
            .getLookup()
            .then((branches) => {
                this.branchesSubject.next(branches);
                const current = this.currentBranchSubject.value;
                if (current && !branches.some((b) => b.id === current.id)) {
                    this.setBranch(null);
                }
                return branches;
            })
            .catch((error) => {
                this.loadPromise = null;
                throw error;
            });

        return this.loadPromise;
    }

    requireBranchId(): number {
        const branchId = this.getBranchId();
        if (branchId == null) {
            throw new Error('Please select a branch from the top navigation.');
        }
        return branchId;
    }
}

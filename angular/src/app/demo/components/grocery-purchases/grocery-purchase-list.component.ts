import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import { GroceryPurchaseDto } from 'src/app/demo/api/grocery-purchase';
import { GroceryPurchaseService } from 'src/app/demo/service/grocery-purchase.service';
import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    templateUrl: './grocery-purchase-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class GroceryPurchaseListComponent implements OnInit, OnDestroy {
    groceryPurchases: GroceryPurchaseDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    private branchSub?: Subscription;

    constructor(
        public formatService: AppFormatService,
        private groceryPurchaseService: GroceryPurchaseService,
        private branchContext: BranchContextService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router
    ) {}

    get totalPurchasesAmount(): number {
        return (this.groceryPurchases || []).reduce(
            (sum, item) => sum + (Number(item.purchasePrice) || 0),
            0
        );
    }

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.GroceryPurchasesCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.GroceryPurchasesEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.GroceryPurchasesDelete);

        // Check if current branch is a restaurant branch; if not, redirect to /purchases
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            if (branch && !this.branchContext.isRestaurantBranch()) {
                this.router.navigate(['/purchases']);
                return;
            }
            this.loadGroceryPurchases();
        });
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
    }

    loadGroceryPurchases(): void {
        this.loading = true;
        const currentBranchId = this.branchContext.getBranchId();

        this.groceryPurchaseService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                branchId: currentBranchId ?? undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.groceryPurchases = result.items || [];
                this.totalRecords = result.totalCount || this.groceryPurchases.length;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load grocery purchases',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadGroceryPurchases();
    }

    clearFilter(): void {
        this.keyword = '';
        this.loadGroceryPurchases();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingId = null;
        this.dialogVisible = true;
    }

    openEditDialog(item: GroceryPurchaseDto): void {
        this.editingId = item.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadGroceryPurchases();
    }

    onDelete(item: GroceryPurchaseDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete grocery purchase record for "${item.productName}" (${this.formatCurrency(item.purchasePrice)})?`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.groceryPurchaseService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Deleted',
                            detail: 'Grocery purchase deleted successfully.',
                        });
                        this.loadGroceryPurchases();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete grocery purchase',
                        });
                    });
            },
        });
    }

    formatCurrency(amount: number | undefined): string {
        const val = amount ?? 0;
        return `PKR ${val.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
    }
}

import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Subscription } from 'rxjs';
import { MessageService } from 'primeng/api';
import { BranchDto } from 'src/app/demo/api/branch';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchNotificationSettingService } from 'src/app/demo/service/branch-notification-setting.service';
import { BranchNotificationSettingDto } from 'src/app/demo/api/branch-notification-setting';

@Component({
    selector: 'app-notification-settings',
    templateUrl: './notification-settings.component.html',
    providers: [MessageService]
})
export class NotificationSettingsComponent implements OnInit, OnDestroy, OnChanges {
    @Input() branchId?: number; // Optional Input for embedding
    currentBranch: BranchDto | null = null;
    setting: BranchNotificationSettingDto | null = null;

    loading = false;
    saving = false;

    isEnabled = false;
    notifyOnSale = false;
    notifyOnPurchase = false;
    notifyOnOnlineOrder = false;
    notifyOnExpense = false;

    emailList: string[] = [];
    newEmailInput = '';
    emailInputError = '';

    private branchSub?: Subscription;

    constructor(
        private branchContext: BranchContextService,
        private notificationSettingService: BranchNotificationSettingService,
        private messageService: MessageService
    ) {}

    async ngOnInit(): Promise<void> {
        if (this.branchId) {
            // Embedded mode: use provided branchId
            this.currentBranch = { id: this.branchId } as BranchDto;
            await this.loadSettings(this.branchId);
            return;
        }

        // Standalone mode: use topbar context
        this.loading = true;
        await this.branchContext.ensureLoaded();
        
        this.currentBranch = this.branchContext.getCurrentBranch();

        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            const previousId = this.currentBranch?.id;
            this.currentBranch = branch;
            if (branch?.id && branch.id !== previousId) {
                this.loadSettings(branch.id);
            }
        });

        if (this.currentBranch?.id) {
            await this.loadSettings(this.currentBranch.id);
        } else {
            this.loading = false;
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['branchId'] && !changes['branchId'].isFirstChange()) {
            const newBranchId = changes['branchId'].currentValue;
            if (newBranchId) {
                this.currentBranch = { id: newBranchId } as BranchDto;
                this.loadSettings(newBranchId);
            }
        }
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
    }

    async loadSettings(branchId: number): Promise<void> {
        this.loading = true;
        this.emailInputError = '';
        try {
            const data = await this.notificationSettingService.get(branchId);
            this.setting = data;
            this.isEnabled = data.isEnabled;
            this.notifyOnSale = data.notifyOnSale;
            this.notifyOnPurchase = data.notifyOnPurchase;
            this.notifyOnOnlineOrder = data.notifyOnOnlineOrder;
            this.notifyOnExpense = data.notifyOnExpense;
            this.emailList = [...(data.emailList || [])];
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load branch notification settings.'
            });
        } finally {
            this.loading = false;
        }
    }

    addEmail(): void {
        const raw = (this.newEmailInput || '').trim();
        this.emailInputError = '';

        if (!raw) {
            this.emailInputError = 'Please enter an email address.';
            return;
        }

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(raw)) {
            this.emailInputError = 'Please enter a valid email address.';
            return;
        }

        const normalized = raw.toLowerCase();
        if (this.emailList.some((e) => e.toLowerCase() === normalized)) {
            this.emailInputError = 'This email is already in the recipient list.';
            return;
        }

        const testTotal = [...this.emailList, normalized].join(', ');
        if (testTotal.length > 1000) {
            this.emailInputError = 'Cannot add email: total recipient list exceeds 1000 characters.';
            return;
        }

        this.emailList.push(normalized);
        this.newEmailInput = '';
    }

    removeEmail(index: number): void {
        if (index >= 0 && index < this.emailList.length) {
            this.emailList.splice(index, 1);
        }
    }

    async save(): Promise<void> {
        if (!this.currentBranch?.id) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Branch Selected',
                detail: 'Please select a branch to configure notification settings.'
            });
            return;
        }

        // If there is pending input in the email box, attempt to add it
        if (this.newEmailInput.trim()) {
            this.addEmail();
            if (this.emailInputError) {
                return;
            }
        }

        this.saving = true;
        try {
            const updated = await this.notificationSettingService.createOrUpdate({
                branchId: this.currentBranch.id,
                isEnabled: this.isEnabled,
                notifyOnSale: this.notifyOnSale,
                notifyOnPurchase: this.notifyOnPurchase,
                notifyOnOnlineOrder: this.notifyOnOnlineOrder,
                notifyOnExpense: this.notifyOnExpense,
                emails: this.emailList.join(', '),
                emailList: this.emailList
            });

            this.setting = updated;
            this.isEnabled = updated.isEnabled;
            this.notifyOnSale = updated.notifyOnSale;
            this.notifyOnPurchase = updated.notifyOnPurchase;
            this.notifyOnOnlineOrder = updated.notifyOnOnlineOrder;
            this.notifyOnExpense = updated.notifyOnExpense;
            this.emailList = [...(updated.emailList || [])];

            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Notification settings saved successfully.'
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Save Failed',
                detail: err?.message || 'Could not save notification settings.'
            });
        } finally {
            this.saving = false;
        }
    }
}

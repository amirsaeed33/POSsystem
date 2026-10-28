import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';
import { UnpaidTakeawayOrderDto } from '../api/restaurant-order';
import { RestaurantOrderService } from './restaurant-order.service';
import { BranchContextService } from './branch-context.service';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root',
})
export class RestaurantTakeawayAlarmService implements OnDestroy {
    private readonly unpaidOrdersSubject = new BehaviorSubject<UnpaidTakeawayOrderDto[]>([]);
    public readonly unpaidOrders$: Observable<UnpaidTakeawayOrderDto[]> = this.unpaidOrdersSubject.asObservable();

    private readonly audioBlockedSubject = new BehaviorSubject<boolean>(false);
    public readonly audioBlocked$: Observable<boolean> = this.audioBlockedSubject.asObservable();

    public isMuted = false;
    private pollTimer: any = null;
    private branchSub?: Subscription;
    private audioCtx: AudioContext | null = null;
    private lastAudioPlayTime = 0;

    constructor(
        private restaurantOrderService: RestaurantOrderService,
        private branchContext: BranchContextService,
        private authService: AuthService
    ) {
        this.init();
    }

    private init(): void {
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            const isRestaurant = branch && Number(branch.businessType) === 2;
            const isWaiter = this.authService.isWaiter();

            // Active ONLY for Restaurant admins/staff (NOT waiters, and NOT non-restaurant)
            if (isRestaurant && !isWaiter) {
                this.startPolling();
                this.checkImmediately();
            } else {
                this.stopPolling();
                this.unpaidOrdersSubject.next([]);
            }
        });
    }

    public startPolling(): void {
        this.stopPolling();
        // 60,000 ms = 1 minute recurring check
        this.pollTimer = setInterval(() => {
            this.checkImmediately();
        }, 60000);
    }

    public stopPolling(): void {
        if (this.pollTimer) {
            clearInterval(this.pollTimer);
            this.pollTimer = null;
        }
    }

    public async checkImmediately(): Promise<void> {
        const branch = this.branchContext.getCurrentBranch();
        const isRestaurant = branch && Number(branch.businessType) === 2;
        const isWaiter = this.authService.isWaiter();

        if (!isRestaurant || isWaiter || !branch?.id) {
            this.unpaidOrdersSubject.next([]);
            return;
        }

        try {
            const orders = await this.restaurantOrderService.getUnpaidTakeawayOrders(branch.id);
            this.unpaidOrdersSubject.next(orders || []);

            if (orders && orders.length > 0 && !this.isMuted) {
                this.triggerAlarmSound();
            }
        } catch (err) {
            // Silently fail polling to prevent console noise
        }
    }

    public toggleMute(): boolean {
        this.isMuted = !this.isMuted;
        return this.isMuted;
    }

    public enableAudio(): void {
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;

            if (!this.audioCtx) {
                this.audioCtx = new AudioContextClass();
            }
            if (this.audioCtx.state === 'suspended') {
                this.audioCtx.resume();
            }
            this.audioBlockedSubject.next(false);
            this.playSynthesizedBeep(true);
        } catch {
            this.audioBlockedSubject.next(true);
        }
    }

    private triggerAlarmSound(): void {
        const now = Date.now();
        // Throttle sound to prevent overlapping bursts (min 15s between triggers if triggered multiple times)
        if (now - this.lastAudioPlayTime < 15000) {
            return;
        }
        this.lastAudioPlayTime = now;
        this.playSynthesizedBeep(false);
    }

    private playSynthesizedBeep(isTestPreview = false): void {
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;

            if (!this.audioCtx) {
                this.audioCtx = new AudioContextClass();
            }

            if (this.audioCtx.state === 'suspended') {
                this.audioCtx.resume().catch(() => {
                    this.audioBlockedSubject.next(true);
                });
                if (this.audioCtx.state === 'suspended') {
                    this.audioBlockedSubject.next(true);
                    return;
                }
            }

            this.audioBlockedSubject.next(false);
            const ctx = this.audioCtx;
            const startTime = ctx.currentTime;

            // Two distinct chime bursts for kitchen takeaway alert
            const notes = isTestPreview ? [587.33, 880.00] : [659.25, 783.99, 1046.50, 659.25, 1046.50];
            let offset = 0;

            notes.forEach((freq, index) => {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();

                osc.type = index % 2 === 0 ? 'sine' : 'triangle';
                osc.frequency.setValueAtTime(freq, startTime + offset);

                const volume = isTestPreview ? 0.2 : 0.35;
                gain.gain.setValueAtTime(volume, startTime + offset);
                gain.gain.exponentialRampToValueAtTime(0.001, startTime + offset + 0.22);

                osc.connect(gain);
                gain.connect(ctx.destination);

                osc.start(startTime + offset);
                osc.stop(startTime + offset + 0.22);

                offset += 0.18;
            });
        } catch {
            this.audioBlockedSubject.next(true);
        }
    }

    ngOnDestroy(): void {
        this.stopPolling();
        if (this.branchSub) {
            this.branchSub.unsubscribe();
        }
        if (this.audioCtx && this.audioCtx.state !== 'closed') {
            try {
                this.audioCtx.close();
            } catch {}
        }
    }
}

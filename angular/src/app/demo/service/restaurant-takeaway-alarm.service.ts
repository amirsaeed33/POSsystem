import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';
import { UnpaidTakeawayOrderDto } from '../api/restaurant-order';
import { RestaurantOrderService } from './restaurant-order.service';
import { BranchContextService } from './branch-context.service';
import { AuthService } from './auth.service';
import { AppSoundService } from './app-sound.service';

@Injectable({
    providedIn: 'root',
})
export class RestaurantTakeawayAlarmService implements OnDestroy {
    private readonly unpaidOrdersSubject = new BehaviorSubject<UnpaidTakeawayOrderDto[]>([]);
    public readonly unpaidOrders$: Observable<UnpaidTakeawayOrderDto[]> = this.unpaidOrdersSubject.asObservable();

    private readonly audioBlockedSubject = new BehaviorSubject<boolean>(false);
    public readonly audioBlocked$: Observable<boolean> = this.audioBlockedSubject.asObservable();

    private pollTimer: any = null;
    private branchSub?: Subscription;
    private muteSub?: Subscription;
    private audioCtx: AudioContext | null = null;
    private lastAudioPlayTime = 0;
    /** Active alarm oscillators so mute can cut them off immediately. */
    private activeOscillators: OscillatorNode[] = [];

    constructor(
        private restaurantOrderService: RestaurantOrderService,
        private branchContext: BranchContextService,
        private authService: AuthService,
        private appSound: AppSoundService
    ) {
        this.muteSub = this.appSound.muted$.subscribe((muted) => {
            if (muted) {
                this.stopAlarmSound();
            }
        });
        this.init();
    }

    /** Proxy global mute so existing templates keep working. */
    get isMuted(): boolean {
        return this.appSound.isMuted;
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
        // 12,000 ms (12 seconds) recurring check for snappy automatic KOT / reminder refresh
        this.pollTimer = setInterval(() => {
            this.checkImmediately();
        }, 12000);
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

            if (orders && orders.length > 0 && this.appSound.canPlay()) {
                this.triggerAlarmSound();
            }
        } catch (err) {
            // Silently fail polling to prevent console noise
        }
    }

    public toggleMute(): boolean {
        const muted = this.appSound.toggleMute();
        if (muted) {
            this.stopAlarmSound();
        }
        return muted;
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
            if (this.appSound.canPlay()) {
                this.playSynthesizedBeep(true);
            }
        } catch {
            this.audioBlockedSubject.next(true);
        }
    }

    /** Immediately stop all takeaway alarm tones. */
    public stopAlarmSound(): void {
        for (const osc of this.activeOscillators) {
            try {
                osc.stop();
                osc.disconnect();
            } catch {
                /* already stopped */
            }
        }
        this.activeOscillators = [];

        if (this.audioCtx && this.audioCtx.state === 'running') {
            try {
                void this.audioCtx.suspend();
            } catch {
                /* ignore */
            }
        }
    }

    private triggerAlarmSound(): void {
        if (!this.appSound.canPlay()) {
            return;
        }
        const now = Date.now();
        // Throttle sound to prevent overlapping bursts (min 15s between triggers if triggered multiple times)
        if (now - this.lastAudioPlayTime < 15000) {
            return;
        }
        this.lastAudioPlayTime = now;
        this.playSynthesizedBeep(false);
    }

    private playSynthesizedBeep(isTestPreview = false): void {
        if (!this.appSound.canPlay()) {
            return;
        }

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

            if (!this.appSound.canPlay()) {
                return;
            }

            this.audioBlockedSubject.next(false);
            const ctx = this.audioCtx;
            const startTime = ctx.currentTime;

            const notes = isTestPreview ? [587.33, 880.0] : [659.25, 783.99, 1046.5, 659.25, 1046.5];
            let offset = 0;
            this.activeOscillators = [];

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
                this.activeOscillators.push(osc);

                osc.onended = () => {
                    this.activeOscillators = this.activeOscillators.filter((o) => o !== osc);
                };

                offset += 0.18;
            });
        } catch {
            this.audioBlockedSubject.next(true);
        }
    }

    ngOnDestroy(): void {
        this.stopPolling();
        this.stopAlarmSound();
        if (this.branchSub) {
            this.branchSub.unsubscribe();
        }
        if (this.muteSub) {
            this.muteSub.unsubscribe();
        }
        if (this.audioCtx && this.audioCtx.state !== 'closed') {
            try {
                this.audioCtx.close();
            } catch {}
        }
    }
}

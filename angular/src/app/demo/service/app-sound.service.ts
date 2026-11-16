import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

/** Shared mute for every POS / storefront alarm, chime, and notification beep. */
const MUTE_STORAGE_KEY = 'smartpos_alarms_muted';
/** Legacy takeaway-only key — migrated into the global flag. */
const LEGACY_TAKEAWAY_MUTE_KEY = 'smartpos_takeaway_alarm_muted';

@Injectable({
    providedIn: 'root',
})
export class AppSoundService {
    private readonly mutedSubject: BehaviorSubject<boolean>;
    public readonly muted$: Observable<boolean>;

    constructor() {
        const legacy = localStorage.getItem(LEGACY_TAKEAWAY_MUTE_KEY);
        const stored = localStorage.getItem(MUTE_STORAGE_KEY);
        const initial =
            stored === 'true' ||
            (stored === null && legacy === 'true');
        this.mutedSubject = new BehaviorSubject<boolean>(initial);
        this.muted$ = this.mutedSubject.asObservable();
        if (initial && stored === null) {
            localStorage.setItem(MUTE_STORAGE_KEY, 'true');
        }
    }

    get isMuted(): boolean {
        return this.mutedSubject.value;
    }

    /** True when any notification / alarm sound is allowed to play. */
    canPlay(): boolean {
        return !this.isMuted;
    }

    toggleMute(): boolean {
        const next = !this.isMuted;
        this.setMuted(next);
        return next;
    }

    setMuted(muted: boolean): void {
        this.mutedSubject.next(!!muted);
        localStorage.setItem(MUTE_STORAGE_KEY, String(!!muted));
        // Keep legacy key in sync so older takeaway-only readers stay aligned.
        localStorage.setItem(LEGACY_TAKEAWAY_MUTE_KEY, String(!!muted));
    }

    /** Short “item added to cart” chime for the online storefront. */
    playAddToCartChime(): void {
        if (!this.canPlay()) {
            return;
        }
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) {
                return;
            }
            const ctx = new AudioContextClass();
            const now = ctx.currentTime;

            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(880, now); // A5
            osc.frequency.exponentialRampToValueAtTime(1318.51, now + 0.08); // E6
            gain.gain.setValueAtTime(0.16, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.22);
        } catch {
            /* ignore autoplay / AudioContext errors */
        }
    }
}

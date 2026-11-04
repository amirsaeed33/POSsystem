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
}

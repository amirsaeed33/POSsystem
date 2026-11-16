import { Component, OnDestroy, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import {
    OnlineUserDto,
    UserPresenceService,
} from 'src/app/demo/service/user-presence.service';

@Component({
    selector: 'app-admin-log',
    templateUrl: './admin-log.component.html',
    providers: [MessageService],
})
export class AdminLogComponent implements OnInit, OnDestroy {
    users: OnlineUserDto[] = [];
    loading = false;
    keyword = '';
    now = Date.now();
    private timer: ReturnType<typeof setInterval> | null = null;
    private clock: ReturnType<typeof setInterval> | null = null;

    constructor(
        private presence: UserPresenceService,
        private messageService: MessageService
    ) {}

    get isHost(): boolean {
        return BranchContextService.isHostAdminSession();
    }

    get visibleUsers(): OnlineUserDto[] {
        const q = (this.keyword || '').trim().toLowerCase();
        if (!q) {
            return this.users;
        }
        return this.users.filter((u) =>
            [u.fullName, u.userName, u.branchName, u.tenancyName, u.roleNames]
                .filter(Boolean)
                .some((v) => String(v).toLowerCase().includes(q))
        );
    }

    ngOnInit(): void {
        void this.load();
        this.timer = setInterval(() => void this.load(false), 20_000);
        this.clock = setInterval(() => {
            this.now = Date.now();
        }, 1000);
    }

    ngOnDestroy(): void {
        if (this.timer) {
            clearInterval(this.timer);
        }
        if (this.clock) {
            clearInterval(this.clock);
        }
    }

    async load(showSpinner = true): Promise<void> {
        if (showSpinner) {
            this.loading = true;
        }
        try {
            await this.presence.ping();
            this.users = await this.presence.getOnline();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load signed-in users',
            });
        } finally {
            this.loading = false;
        }
    }

    formatWhen(value?: string | null): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleString([], {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    }

    /** Time this person has been signed in, from their login until now. */
    timeSpent(row: OnlineUserDto): string {
        if (!row.loginTime) {
            return '—';
        }
        const start = new Date(row.loginTime).getTime();
        if (isNaN(start)) {
            return '—';
        }
        const ms = Math.max(0, this.now - start);
        const totalMinutes = Math.floor(ms / 60000);
        const days = Math.floor(totalMinutes / (60 * 24));
        const hours = Math.floor((totalMinutes % (60 * 24)) / 60);
        const minutes = totalMinutes % 60;
        if (days > 0) {
            return hours > 0 ? `${days}d ${hours}h` : `${days}d`;
        }
        if (hours > 0) {
            return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
        }
        if (minutes > 0) {
            return `${minutes}m`;
        }
        return `${Math.floor(ms / 1000)}s`;
    }
}

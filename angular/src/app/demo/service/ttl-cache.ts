/** Simple branch-aware TTL cache for Angular services. */
export class TtlCache<T> {
    private entries = new Map<string, { data: T; timestamp: number }>();

    constructor(private readonly ttlMs: number) {}

    get(key: string): T | null {
        const entry = this.entries.get(key);
        if (!entry) {
            return null;
        }
        if (Date.now() - entry.timestamp >= this.ttlMs) {
            this.entries.delete(key);
            return null;
        }
        return entry.data;
    }

    set(key: string, data: T): void {
        this.entries.set(key, { data, timestamp: Date.now() });
    }

    clear(): void {
        this.entries.clear();
    }

    delete(key: string): void {
        this.entries.delete(key);
    }
}

export function branchCacheKey(branchId?: number | null, suffix = ''): string {
    const b = branchId != null && branchId > 0 ? String(branchId) : 'none';
    return suffix ? `b:${b}:${suffix}` : `b:${b}`;
}

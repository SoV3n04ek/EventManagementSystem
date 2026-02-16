import { Injectable } from '@angular/core';
import { Observable, shareReplay, Subject, switchMap, startWith } from 'rxjs';

/**
 * CacheService — Request Deduplicator
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * Uses `Map<string, Observable<T>>` with `shareReplay(1)` to:
 *
 * 1. PREVENT REQUEST EXPLOSIONS: If 3 components request the same
 *    key before the HTTP call completes, only ONE request is made.
 *    All subscribers receive the same emission via `shareReplay`.
 *
 * 2. STALE-WHILE-REVALIDATE: Cached Observables immediately replay
 *    the last value to new subscribers while a `refresh()` call
 *    triggers a fresh HTTP call underneath.
 *
 * 3. MANUAL INVALIDATION: `invalidate(key)` removes the cached
 *    Observable, forcing the next `get()` to create a new one.
 * ─────────────────────────────────────────────────────────────
 */
@Injectable({ providedIn: 'root' })
export class CacheService {
    private readonly cache = new Map<string, Observable<any>>();
    private readonly refreshTriggers = new Map<string, Subject<void>>();

    /**
     * Returns a cached, shared Observable for the given key.
     * If no entry exists, creates one using the `factory`.
     *
     * shareReplay(1) ensures:
     *   - Only 1 HTTP call even with multiple concurrent subscribers
     *   - Late subscribers get the last emitted value immediately
     */
    get<T>(key: string, factory: () => Observable<T>): Observable<T> {
        if (!this.cache.has(key)) {
            const trigger = new Subject<void>();
            this.refreshTriggers.set(key, trigger);

            const shared$ = trigger.pipe(
                startWith(undefined),
                switchMap(() => factory()),
                shareReplay({ bufferSize: 1, refCount: true })
            );

            this.cache.set(key, shared$);
        }

        return this.cache.get(key)! as Observable<T>;
    }

    /**
     * Force a re-fetch for a specific key.
     * Existing subscribers receive the new value automatically.
     */
    refresh(key: string): void {
        this.refreshTriggers.get(key)?.next();
    }

    /**
     * Remove a cached entry entirely.
     * Next `get()` call will create a fresh Observable.
     */
    invalidate(key: string): void {
        this.cache.delete(key);
        this.refreshTriggers.get(key)?.complete();
        this.refreshTriggers.delete(key);
    }

    /**
     * Invalidate all entries matching a prefix.
     * Useful for clearing all event-related caches at once.
     */
    invalidateByPrefix(prefix: string): void {
        for (const key of [...this.cache.keys()]) {
            if (key.startsWith(prefix)) {
                this.invalidate(key);
            }
        }
    }

    /** Check if a key has a cached Observable. */
    has(key: string): boolean {
        return this.cache.has(key);
    }
}

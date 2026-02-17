import { Injectable, signal, inject, NgZone } from '@angular/core';

/**
 * ToastService — Zoneless-Ready Global Notification
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. Uses a writable signal for reactive state management.
 * 2. NgZone.runOutsideAngular() for setTimeout to prevent
 *    waking the change detection tree on every auto-dismiss.
 * 3. This makes the toast fully zoneless-compatible.
 * ─────────────────────────────────────────────────────────────
 */

export type ToastType = 'success' | 'error';

export interface ToastState {
    msg: string;
    type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly zone = inject(NgZone);

    // Private writable signal
    private readonly state = signal<ToastState | null>(null);

    // Public readonly accessor
    readonly current = this.state.asReadonly();

    private dismissTimeout?: number;

    /**
     * Show a toast message with auto-dismiss after 3000ms
     */
    show(msg: string, type: ToastType = 'success'): void {
        // Clear any pending dismissal
        if (this.dismissTimeout) {
            clearTimeout(this.dismissTimeout);
        }

        // Set the new toast state
        this.state.set({ msg, type });

        // Auto-dismiss OUTSIDE Angular zone to prevent change detection cycles
        this.zone.runOutsideAngular(() => {
            this.dismissTimeout = window.setTimeout(() => {
                // Re-enter zone only when clearing (state change)
                this.zone.run(() => {
                    this.state.set(null);
                });
            }, 3000);
        });
    }

    /**
     * Manually dismiss the current toast
     */
    dismiss(): void {
        if (this.dismissTimeout) {
            clearTimeout(this.dismissTimeout);
        }
        this.state.set(null);
    }
}

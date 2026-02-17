import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../services/toast.service';

/**
 * ToastComponent — Reactive Global Notification UI
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. Standalone component with OnPush change detection.
 * 2. Subscribes to ToastService.current() signal.
 * 3. Uses @if block for conditional rendering.
 * 4. Slide-in animation via CSS transitions.
 * ─────────────────────────────────────────────────────────────
 */
@Component({
    selector: 'app-toast',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './toast.component.html',
    styleUrl: './toast.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastComponent {
    protected readonly toastService = inject(ToastService);

    // Expose the current toast signal to template
    readonly toast = this.toastService.current;

    onDismiss(): void {
        this.toastService.dismiss();
    }
}

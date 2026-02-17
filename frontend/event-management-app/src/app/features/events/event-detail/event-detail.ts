import {
  Component,
  ChangeDetectionStrategy,
  computed,
  inject,
  input
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Event } from '../../../models/event';

/**
 * EventDetailComponent — Single Event View with Toast Integration
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. Route param `id` bound as input() signal via
 *    withComponentInputBinding() — no ActivatedRoute needed.
 * 2. rxResource keyed on id signal — auto-refetches when
 *    navigating between event details.
 * 3. ToastService for global feedback (replaces local actionError)
 * 4. Mutations (join/leave/delete) call .reload() on success.
 * ─────────────────────────────────────────────────────────────
 */
@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventDetailComponent {
  private readonly router = inject(Router);
  private readonly eventService = inject(EventService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  // ── Route param bound via withComponentInputBinding() ──
  readonly id = input<string>();

  /** Parsed numeric ID from route param */
  private readonly eventId = computed(() => {
    const raw = this.id();
    return raw ? Number(raw) : null;
  });

  /** rxResource: auto-fetches when eventId() changes (Angular 20.3 API) */
  readonly eventResource = rxResource<Event | null, number | null>({
    params: () => this.eventId(),
    stream: ({ params: id }) => {
      if (!id || isNaN(id)) {
        throw new Error('Invalid event ID');
      }
      return this.eventService.getEventById(id);
    }
  });

  // ── Derived computed signals ──
  readonly currentUser = computed(() => this.authService.currentUser());
  readonly isAuthenticated = computed(() => this.authService.isAuthenticated());

  // Identity-aware flags from server-side enriched DTO
  readonly isOrganizer = computed(() => this.eventResource.value()?.isOrganizer ?? false);
  readonly isParticipant = computed(() => this.eventResource.value()?.isParticipant ?? false);

  readonly canJoin = computed(() => {
    const event = this.eventResource.value();
    if (!event || !this.isAuthenticated()) return false;

    return !this.isParticipant() && !this.isOrganizer() && !event.isFull;
  });

  readonly canLeave = computed(() => {
    return this.isAuthenticated() && this.isParticipant() && !this.isOrganizer();
  });

  readonly canEdit = computed(() => this.isOrganizer());
  readonly canDelete = computed(() => this.isOrganizer());

  // ── Action Handlers (NO ARGUMENTS - use internal signals) ──

  joinEvent(): void {
    const id = this.eventId();
    if (!id) return;

    this.eventService.joinEvent(id).subscribe({
      next: () => {
        this.toastService.show('Successfully joined the event!', 'success');
        this.eventResource.reload();
      },
      error: (err) => {
        console.error('Error joining event:', err);
        this.toastService.show(
          err?.error?.message || 'Failed to join event',
          'error'
        );
      }
    });
  }

  leaveEvent(): void {
    const id = this.eventId();
    if (!id) return;

    this.eventService.leaveEvent(id).subscribe({
      next: () => {
        this.toastService.show('You have left the event', 'success');
        this.eventResource.reload();
      },
      error: (err) => {
        console.error('Error leaving event:', err);
        this.toastService.show(
          err?.error?.message || 'Failed to leave event',
          'error'
        );
      }
    });
  }

  editEvent(): void {
    const id = this.id();
    if (id) {
      this.router.navigate(['/events', id, 'edit']);
    }
  }

  deleteEvent(): void {
    const id = this.eventId();
    if (!id) return;

    if (!confirm('Are you sure you want to delete this event? This action cannot be undone.')) {
      return;
    }

    this.eventService.deleteEvent(id).subscribe({
      next: () => {
        this.toastService.show('Event deleted successfully', 'success');
        this.router.navigate(['/events']);
      },
      error: (err) => {
        console.error('Error deleting event:', err);
        this.toastService.show(
          err?.error?.message || 'Failed to delete event',
          'error'
        );
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }

  /**
   * Helper: Generate initials from participant name
   */
  getInitials(name: string): string {
    return (name || '')
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }
}
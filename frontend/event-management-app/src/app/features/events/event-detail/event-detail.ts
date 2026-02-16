import {
  Component,
  ChangeDetectionStrategy,
  computed,
  signal,
  inject,
  input
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { Event } from '../../../models/event';

/**
 * EventDetailComponent — Single Event View
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. Route param `id` bound as input() signal via
 *    withComponentInputBinding() — no ActivatedRoute needed.
 * 2. rxResource keyed on id signal — auto-refetches when
 *    navigating between event details.
 * 3. Loading/error states from resource — no BehaviorSubject.
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

  // ── Route param bound via withComponentInputBinding() ──
  readonly id = input<string>();

  /** Parsed numeric ID from route param */
  private readonly eventId = computed(() => {
    const raw = this.id();
    return raw ? Number(raw) : null;
  });

  // ── Action error (for join/leave/delete failures) ──
  readonly actionError = signal('');

  /** rxResource: auto-fetches when eventId() changes */
  readonly eventResource = rxResource<Event, number | null>({
    params: () => this.eventId(),
    stream: ({ params: id }) => {
      if (!id || isNaN(id)) {
        throw new Error('Invalid event ID');
      }
      return this.eventService.getEventById(id);
    }
  });

  // ── Derived State ──
  readonly currentUser = computed(() => this.authService.currentUser());

  readonly isOrganizer = computed(() => {
    const user = this.currentUser();
    const event: Event | undefined = this.eventResource.value();
    return !!user && !!event && event.organizerId === user.id;
  });

  readonly isParticipant = computed(() => {
    const user = this.currentUser();
    const event: Event | undefined = this.eventResource.value();
    return !!user && !!event && (event.participants?.some((p: any) => p.id === user.id) ?? false);
  });

  // ── Actions ──

  joinEvent(eventId: number): void {
    this.eventService.joinEvent(eventId).subscribe({
      next: () => this.eventResource.reload(),
      error: (err) => {
        this.actionError.set(err?.error?.message || 'Failed to join event');
      }
    });
  }

  leaveEvent(eventId: number): void {
    this.eventService.leaveEvent(eventId).subscribe({
      next: () => this.eventResource.reload(),
      error: (err) => {
        this.actionError.set(err?.error?.message || 'Failed to leave event');
      }
    });
  }

  editEvent(eventId: number): void {
    this.router.navigate(['/events', eventId, 'edit']);
  }

  deleteEvent(eventId: number): void {
    if (confirm('Are you sure you want to delete this event?')) {
      this.eventService.deleteEvent(eventId).subscribe({
        next: () => this.router.navigate(['/events']),
        error: (err) => {
          this.actionError.set(err?.error?.message || 'Failed to delete event');
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }

  getInitials(name: string): string {
    return (name || '')
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }
}
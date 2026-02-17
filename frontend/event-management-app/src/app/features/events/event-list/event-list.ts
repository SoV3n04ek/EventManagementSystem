import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { rxResource } from '@angular/core/rxjs-interop';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { EventCardComponent } from '../../../shared/components/event-card/event-card';
import { EventListItem } from '../../../models/event';

/**
 * EventListComponent — Public Events Listing with Toast Integration
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. rxResource for data fetching — automatically provides
 *    .value(), .isLoading(), .error(), .status() signals.
 * 2. ToastService for global feedback (replaces local states)
 * 3. computed() for filtering — pure derivation, no side effects.
 * 4. OnPush — only re-renders when signal values change.
 * ─────────────────────────────────────────────────────────────
 */
@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, FormsModule, EventCardComponent],
  templateUrl: './event-list.html',
  styleUrl: './event-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventListComponent {
  private readonly eventService = inject(EventService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  // ── Reactive State ──
  readonly searchQuery = signal('');

  /** rxResource auto-fetches on creation (Angular 20.3 API) */
  readonly eventsResource = rxResource<EventListItem[], void>({
    stream: () => this.eventService.getPublicEvents()
  });

  /** Derived: filter events by search query. Only re-evaluates when dependencies change. */
  readonly filteredEvents = computed<EventListItem[]>(() => {
    const events: EventListItem[] = this.eventsResource.value() ?? [];
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return events;

    return events.filter(event =>
      event.name.toLowerCase().includes(query) ||
      event.location.toLowerCase().includes(query) ||
      event.shortDescription.toLowerCase().includes(query)
    );
  });

  readonly isAuthenticated = computed(() => this.authService.isAuthenticated());

  // ── Actions ──

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
  }

  handleJoinEvent(eventId: number): void {
    this.eventService.joinEvent(eventId).subscribe({
      next: () => {
        this.toastService.show('Successfully joined the event!', 'success');
        this.eventsResource.reload();
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

  handleLeaveEvent(eventId: number): void {
    this.eventService.leaveEvent(eventId).subscribe({
      next: () => {
        this.toastService.show('You have left the event', 'success');
        this.eventsResource.reload();
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
}
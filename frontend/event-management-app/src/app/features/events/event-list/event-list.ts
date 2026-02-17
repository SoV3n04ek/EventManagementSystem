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
import { EventCardComponent } from '../../../shared/components/event-card/event-card';
import { EventListItem } from '../../../models/event';

/**
 * EventListComponent — Public Events Listing
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. rxResource for data fetching — automatically provides
 *    .value(), .isLoading(), .error(), .status() signals.
 *    Eliminates BehaviorSubject + async pipe boilerplate.
 * 2. signal('') for search query — triggers computed() re-eval.
 * 3. computed() for filtering — pure derivation, no side effects.
 *    Re-evaluates only when events or searchQuery change.
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

  // ── Reactive State ──
  readonly searchQuery = signal('');
  readonly successMessage = signal('');
  readonly errorMessage = signal('');

  /** rxResource auto-fetches on creation. Provides .value(), .isLoading(), .error() */
  readonly eventsResource = rxResource<EventListItem[], unknown>({
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
        this.successMessage.set('Successfully joined the event!');
        this.eventsResource.reload();
        setTimeout(() => this.successMessage.set(''), 3000);
      },
      error: (err) => {
        const message = err.error?.message || err.error?.error || 'Failed to join event';
        this.errorMessage.set(message);
        setTimeout(() => this.errorMessage.set(''), 5000);
      }
    });
  }

  handleLeaveEvent(eventId: number): void {
    this.eventService.leaveEvent(eventId).subscribe({
      next: () => {
        this.successMessage.set('You have left the event.');
        this.eventsResource.reload(); // Refresh the list to flip the UI state
        setTimeout(() => this.successMessage.set(''), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to leave event');
        setTimeout(() => this.errorMessage.set(''), 5000);
      }
    });
  }
}
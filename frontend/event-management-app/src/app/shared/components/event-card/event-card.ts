import {
  Component,
  ChangeDetectionStrategy,
  computed,
  input,
  output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventListItem } from '../../../models/event';
import { RouterLink } from '@angular/router';

/**
 * EventCardComponent — Pure presentational card with identity-aware state.
 *
 * Signal-based inputs enable fine-grained reactivity:
 * - input() signals participate in the reactive graph (computed can depend on them)
 * - output() signals provide type-safe event emission
 * - OnPush + Signals = zoneless-ready, only re-renders when inputs actually change
 * - computed() for button state derived from DTO flags
 */
@Component({
  selector: 'app-event-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './event-card.html',
  styleUrl: './event-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventCardComponent {
  // ── Signal Inputs ──
  readonly event = input.required<EventListItem>();

  // ── Signal Outputs ──
  readonly join = output<number>();
  readonly leave = output<number>();

  // ── Computed State (derived from DTO flags) ──
  readonly isJoined = computed(() => this.event().isParticipant);
  readonly isOrganizer = computed(() => this.event().isOrganizer);

  // ── Computed (memoized, runs only when event() changes) ──
  readonly formattedDate = computed(() => {
    const date = new Date(this.event().eventDate);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  readonly formattedTime = computed(() => {
    const date = new Date(this.event().eventDate);
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  });

  readonly participantLabel = computed(() => {
    const count = this.event().participantCount;
    return `${count} participant${count !== 1 ? 's' : ''}`;
  });

  onJoin(): void {
    this.join.emit(this.event().id);
  }

  onLeave(): void {
    this.leave.emit(this.event().id);
  }
}
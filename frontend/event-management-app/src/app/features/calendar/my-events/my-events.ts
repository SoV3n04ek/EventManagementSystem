import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { EventService } from '../../../core/services/event.service';
import { CalendarViewDto, CalendarEventDto } from '../../../models/event';

/**
 * MyEventsComponent — Personal Calendar View
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. signal() for currentDate and viewType — user-driven state.
 * 2. computed() for dateRange — derives start/end from
 *    currentDate + viewType without side effects.
 * 3. rxResource with request tracking dateRange signal —
 *    auto-refetches when user navigates months/weeks.
 * 4. computed() for currentMonthYear label.
 * ─────────────────────────────────────────────────────────────
 */
@Component({
  selector: 'app-my-events',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-events.html',
  styleUrl: './my-events.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyEventsComponent {
  private readonly eventService = inject(EventService);

  // ── User-driven State ──
  readonly currentDate = signal(new Date());
  readonly viewType = signal<'month' | 'week'>('month');

  readonly weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  // ── Derived: date range for the current view ──
  readonly dateRange = computed(() => {
    const date = this.currentDate();
    const type = this.viewType();

    if (type === 'week') {
      const startOfWeek = new Date(date);
      startOfWeek.setDate(date.getDate() - date.getDay());
      startOfWeek.setHours(0, 0, 0, 0);
      const endOfWeek = new Date(startOfWeek);
      endOfWeek.setDate(startOfWeek.getDate() + 6);
      endOfWeek.setHours(23, 59, 59, 999);
      return { start: startOfWeek, end: endOfWeek, viewType: type };
    }

    // Month view
    const startOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);
    const endOfMonth = new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59, 999);
    return { start: startOfMonth, end: endOfMonth, viewType: type };
  });

  /** rxResource: auto-refetches when dateRange changes */
  readonly calendarResource = rxResource<CalendarViewDto, { start: Date; end: Date; viewType: string }>({
    params: () => this.dateRange(),
    stream: ({ params: range }) =>
      this.eventService.getCalendarEvents(range.start, range.end, range.viewType)
  });

  readonly currentMonthYear = computed(() => {
    const date = this.currentDate();
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  // ── Navigation ──

  previousPeriod(): void {
    const current = this.currentDate();
    if (this.viewType() === 'month') {
      this.currentDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
    } else {
      const prev = new Date(current);
      prev.setDate(current.getDate() - 7);
      this.currentDate.set(prev);
    }
  }

  nextPeriod(): void {
    const current = this.currentDate();
    if (this.viewType() === 'month') {
      this.currentDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
    } else {
      const next = new Date(current);
      next.setDate(current.getDate() + 7);
      this.currentDate.set(next);
    }
  }

  goToToday(): void {
    this.currentDate.set(new Date());
  }

  setViewType(type: 'month' | 'week'): void {
    this.viewType.set(type);
  }

  // ── Calendar Day Helpers ──

  getCalendarDays(data: CalendarViewDto): Array<{
    date: Date;
    isCurrentMonth: boolean;
    isToday: boolean;
    events: CalendarEventDto[];
  }> {
    const days: Array<{
      date: Date;
      isCurrentMonth: boolean;
      isToday: boolean;
      events: CalendarEventDto[];
    }> = [];

    const current = this.currentDate();
    const year = current.getFullYear();
    const month = current.getMonth();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // Start from Sunday of the first week
    const firstDay = new Date(year, month, 1);
    const startDate = new Date(firstDay);
    startDate.setDate(firstDay.getDate() - firstDay.getDay());

    // End on Saturday of the last week
    const lastDay = new Date(year, month + 1, 0);
    const endDate = new Date(lastDay);
    endDate.setDate(lastDay.getDate() + (6 - lastDay.getDay()));

    const cursor = new Date(startDate);
    while (cursor <= endDate) {
      const dayDate = new Date(cursor);
      const dayStart = new Date(dayDate);
      dayStart.setHours(0, 0, 0, 0);

      days.push({
        date: dayDate,
        isCurrentMonth: dayDate.getMonth() === month,
        isToday:
          dayDate.getDate() === today.getDate() &&
          dayDate.getMonth() === today.getMonth() &&
          dayDate.getFullYear() === today.getFullYear(),
        events: this.getEventsForDay(dayDate, data)
      });

      cursor.setDate(cursor.getDate() + 1);
    }

    return days;
  }

  getEventsForDay(date: Date, data: CalendarViewDto): CalendarEventDto[] {
    if (!data?.events) return [];
    return data.events.filter(event => {
      const eventDate = new Date(event.start);
      return (
        eventDate.getDate() === date.getDate() &&
        eventDate.getMonth() === date.getMonth() &&
        eventDate.getFullYear() === date.getFullYear()
      );
    });
  }

  getWeekDay(dayIndex: number): Date {
    const current = this.currentDate();
    const startOfWeek = new Date(current);
    startOfWeek.setDate(current.getDate() - current.getDay());
    const target = new Date(startOfWeek);
    target.setDate(startOfWeek.getDate() + dayIndex);
    return target;
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return (
      date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear()
    );
  }
}
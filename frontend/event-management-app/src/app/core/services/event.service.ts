import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Event,
  EventListItem,
  CreateEventRequest,
  UpdateEventRequest,
  CalendarViewDto
} from '../../models/event';
import { CacheService } from '../../shared/services/cache.service';

/**
 * EventService — Data Transport Layer
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. inject() over constructor DI — Angular 20.x best practice.
 * 2. Returns Observable<T> for all methods — consumed by
 *    rxResource() in components or direct .subscribe() for mutations.
 * 3. CacheService integration for read operations:
 *    - getPublicEvents() and getEventById() are cached via
 *      shareReplay(1) dedup to prevent request explosions.
 *    - Mutation methods (create/update/delete/join/leave)
 *      invalidate relevant cache keys so the next read is fresh.
 * ─────────────────────────────────────────────────────────────
 */
@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(CacheService);
  private readonly apiUrl = `${environment.apiUrl}/Events`;

  // ── Read Operations (Cached) ──

  getPublicEvents(): Observable<EventListItem[]> {
    return this.cache.get<EventListItem[]>(
      'events:public',
      () => this.http.get<EventListItem[]>(this.apiUrl)
    );
  }

  getEventById(id: number): Observable<Event> {
    return this.cache.get<Event>(
      `events:detail:${id}`,
      () => this.http.get<Event>(`${this.apiUrl}/${id}`)
    );
  }

  getMyEvents(): Observable<EventListItem[]> {
    return this.cache.get<EventListItem[]>(
      'events:my',
      () => this.http.get<EventListItem[]>(`${this.apiUrl}/user/me`)
    );
  }

  getCalendarEvents(
    startDate: Date,
    endDate: Date,
    viewType: string = 'month'
  ): Observable<CalendarViewDto> {
    const params = {
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      viewType
    };
    // Calendar data changes with date range — unique key per range
    const key = `events:calendar:${params.startDate}:${params.endDate}:${viewType}`;
    return this.cache.get<CalendarViewDto>(
      key,
      () => this.http.get<CalendarViewDto>(`${this.apiUrl}/user/me/calendar`, { params })
    );
  }

  // ── Mutation Operations (invalidate cache on success) ──

  createEvent(event: CreateEventRequest): Observable<{ id: number; message: string }> {
    return this.http.post<{ id: number; message: string }>(this.apiUrl, event).pipe(
      tap(() => this.invalidateEventCaches())
    );
  }

  updateEvent(id: number, event: UpdateEventRequest): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/${id}`, event).pipe(
      tap(() => this.invalidateEventCaches(id))
    );
  }

  deleteEvent(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.invalidateEventCaches(id))
    );
  }

  joinEvent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/join`, {}).pipe(
      tap(() => this.invalidateEventCaches(id))
    );
  }

  leaveEvent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/leave`, {}).pipe(
      tap(() => this.invalidateEventCaches(id))
    );
  }

  // ── Cache Management ──

  /** Force refresh the public events list */
  refreshPublicEvents(): void {
    this.cache.refresh('events:public');
  }

  /** Invalidate all event-related caches after a mutation */
  private invalidateEventCaches(eventId?: number): void {
    this.cache.invalidate('events:public');
    this.cache.invalidate('events:my');
    this.cache.invalidateByPrefix('events:calendar');
    if (eventId) {
      this.cache.invalidate(`events:detail:${eventId}`);
    }
  }
}
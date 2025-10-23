import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  Event, 
  EventListItem, 
  CreateEventRequest,
  UpdateEventRequest,
  CalendarViewDto 
} from '../../models/event';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = `${environment.apiUrl}/Events`;

  constructor(private http: HttpClient) {}

  // Get all public events
  getPublicEvents(): Observable<EventListItem[]> {
    return this.http.get<EventListItem[]>(this.apiUrl);
  }

  // Get single event by ID
  getEventById(id: number): Observable<Event> {
    return this.http.get<Event>(`${this.apiUrl}/${id}`);
  }

  // Create new event
  createEvent(event: CreateEventRequest): Observable<{ id: number; message: string }> {
    return this.http.post<{ id: number; message: string }>(this.apiUrl, event);
  }

  // Update event
  updateEvent(id: number, event: UpdateEventRequest): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/${id}`, event);
  }

  // Delete event
  deleteEvent(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  // Join event
  joinEvent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/join`, {});
  }

  // Leave event
  leaveEvent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/leave`, {});
  }

  // Get user's events (organized + participating)
  getMyEvents(): Observable<EventListItem[]> {
    return this.http.get<EventListItem[]>(`${this.apiUrl}/user/me`);
  }

  // Get calendar view
  getCalendarEvents(startDate: Date, endDate: Date, viewType: string = 'month'): Observable<CalendarViewDto> {
    const params = {
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      viewType
    };
    return this.http.get<CalendarViewDto>(`${this.apiUrl}/user/me/calendar`, { params });
  }
}
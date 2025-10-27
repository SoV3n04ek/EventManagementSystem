import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventCardComponent } from '../../../shared/components/event-card/event-card';
import { EventListItem } from '../../../models/event';
import { 
  Observable,
  BehaviorSubject,
  combineLatest,
  switchMap,
  map,
  catchError,
  of,
  debounceTime,
  distinctUntilChanged
} from 'rxjs';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, FormsModule, EventCardComponent],
  templateUrl: './event-list.html',
  styleUrl: './event-list.css'
})
export class EventListComponent implements OnInit {
  private searchQuerySubject = new BehaviorSubject<string>('');
  private refreshTriggerSubject = new BehaviorSubject<void>(undefined);

  searchQuery$ = this.searchQuerySubject.asObservable();

  private eventsData$ = this.refreshTriggerSubject.pipe(
    switchMap(() => 
      this.eventService.getPublicEvents().pipe(
        catchError(error => {
          console.error('Error loading events: ', error);
          return of([]);
        })
      )
    )
  );
 
  filteredEvents$: Observable<EventListItem[]> = combineLatest([
    this.eventsData$,
    this.searchQuerySubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    )
  ]).pipe(
    map(([events, query]) => {
      const searchTerm = query.toLowerCase().trim();
      if (!searchTerm) return events;

      return events.filter(event =>
        event.name.toLowerCase().includes(searchTerm) ||
        event.location.toLowerCase().includes(searchTerm) ||
        event.shortDescription.toLowerCase().includes(searchTerm)
      );
    })
  );

  private successMessageSubject = new BehaviorSubject<string>('');
  successMessage$ = this.successMessageSubject.asObservable();

  private errorMessageSubject = new BehaviorSubject<string>('');
  errorMessage$ = this.errorMessageSubject.asObservable();

  isAuthenticated = computed(() => !!this.authService.currentUser());

  constructor(
    private eventService: EventService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.refreshTriggerSubject.next();
  }

   onSearchChange(query: string): void {
    this.searchQuerySubject.next(query);
  }

  handleJoinEvent(eventId: number): void {
    this.eventService.joinEvent(eventId).subscribe({
      next: () => {
        this.successMessageSubject.next('Successfully joined the event!');
        
        // Refresh events to update participant count
        this.refreshTriggerSubject.next();
        
        // Clear success message after 3 seconds
        setTimeout(() => this.successMessageSubject.next(''), 3000);
      },
      error: (err) => {
        const message = err.error?.message || err.error?.error || 'Failed to join event';
        this.errorMessageSubject.next(message);
        
        // Clear error message after 5 seconds
        setTimeout(() => this.errorMessageSubject.next(''), 5000);
      }
    });
  }

  getEventCount(filtered: EventListItem[], all: EventListItem[]): string {
    return `Showing ${filtered.length} of ${all.length} events`;
  }
}
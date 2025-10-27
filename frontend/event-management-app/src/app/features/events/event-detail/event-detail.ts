import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { defer, BehaviorSubject } from 'rxjs';
import { switchMap, map, catchError, tap, finalize, shareReplay } from 'rxjs/operators';
import { Event } from '../../../models/event';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.css'
})
export class EventDetailComponent {
  private loadingSubject = new BehaviorSubject<boolean>(true);
  private errorSubject = new BehaviorSubject<string>('');

  loading$ = this.loadingSubject.asObservable();
  error$ = this.errorSubject.asObservable();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private authService: AuthService
  ) {}

  event$ = defer(() => this.route.paramMap.pipe(
    map(pm => Number(pm.get('id'))),
    tap(() => {
      this.loadingSubject.next(true);
      this.errorSubject.next('');
    }),
    switchMap(id => {
      if (!id || isNaN(id)) {
        this.errorSubject.next('Invalid event ID');
        this.loadingSubject.next(false);
        return [null];
      }
      
      return this.eventService.getEventById(id).pipe(
        catchError(err => {
          console.error('Error loading event:', err);
          this.errorSubject.next('Failed to load event details');
          return [null];
        }),
        finalize(() => this.loadingSubject.next(false))
      );
    }),
    shareReplay({ bufferSize: 1, refCount: true })
  ));

  get currentUser() {
    return this.authService.currentUser();
  }

  isOrganizer(event: Event): boolean {
    const user = this.currentUser;
    return !!user && !!event && event.organizerId === user.id;
  }

  isParticipant(event: Event): boolean {
    const user = this.currentUser;
    return !!user && !!event && event.participants?.some(p => p.id === user.id);
  }

  // Action methods
  joinEvent(eventId: number): void {
    this.eventService.joinEvent(eventId).subscribe({
      next: () => {
        // Reload event by re-navigating (triggers event$ reload)
        this.router.navigate(['/events', eventId], { 
          queryParams: { refresh: Date.now() } 
        });
      },
      error: (err) => {
        console.error('Failed to join event:', err);
        this.errorSubject.next(err?.error?.message || 'Failed to join event');
      }
    });
  }

  leaveEvent(eventId: number): void {
    this.eventService.leaveEvent(eventId).subscribe({
      next: () => {
        this.router.navigate(['/events', eventId], { 
          queryParams: { refresh: Date.now() } 
        });
      },
      error: (err) => {
        console.error('Failed to leave event:', err);
        this.errorSubject.next(err?.error?.message || 'Failed to leave event');
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
          console.error('Failed to delete event:', err);
          this.errorSubject.next(err?.error?.message || 'Failed to delete event');
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
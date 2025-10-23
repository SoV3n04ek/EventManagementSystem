import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { Event } from '../../../models/event';

@Component({
  selector: 'app-event-detail',
  standalone:true, 
  imports: [CommonModule],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.css'
})
export class EventDetailComponent {
  readonly MAX_SAFE_INTEGER = Number.MAX_SAFE_INTEGER;
  event = signal<Event | null>(null);
  loading = signal(true);
  error = signal('');
  actionLoading = signal(false);
  successMessage = signal('');
  showDeleteModal = signal(false);

  isAuthenticated = computed(() => !!this.authService.currentUser());
  isOrganizer = computed(() => {
    const currentUser = this.authService.currentUser();
    const eventData = this.event();
    return currentUser && eventData && eventData.organizerName === currentUser.name;
  });
  isParticipant = computed(() => {
    const currentUser = this.authService.currentUser();
    const eventData = this.event();
    return currentUser && eventData &&
      eventData.participants.some(p => p.id === currentUser.id);
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const id = +params['id'];
      if (id) {
        this.loadEvent(id);
      }
    });
  }

  loadEvent(id: number): void {
    this.loading.set(true);
    this.error.set('');

    this.eventService.getEventById(id).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load event details');
        this.loading.set(false);
        console.error('Error loading event: ', err);
      }
    });
  }

  joinEvent(): void {
    const eventData = this.event();

    if (!eventData) return;

    this.actionLoading.set(true);
    this.eventService.joinEvent(eventData.id).subscribe({
      next: () => {
        this.successMessage.set("Successfully joined the event!");
        this.loadEvent(eventData.id);
        this.actionLoading.set(false);
        setTimeout(() => this.successMessage.set(''), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to join event');
        this.actionLoading.set(false);
        setTimeout(() => this.error.set(''), 5000);
      }
    });
  }

  leaveEvent(): void {
    const eventData = this.event();
    if (!eventData) return;

    this.actionLoading.set(true);
    this.eventService.leaveEvent(eventData.id).subscribe({
      next: () => {
        this.successMessage.set('Successfully left the event');
        this.loadEvent(eventData.id);
        this.actionLoading.set(false);
        setTimeout(() => this.successMessage.set(''), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to leave event');
        this.actionLoading.set(false);
        setTimeout(() => this.error.set(''), 5000);
      }
    });
  }

  editEvent(): void {
    const eventData = this.event();
    if (eventData) {
      this.router.navigate(['/events', eventData.id, 'edit']);
    }
  }

  confirmDelete(): void {
    this.showDeleteModal.set(true);
  }

  cancelDelete(): void {
    this.showDeleteModal.set(false);
  }

  deleteEvent(): void {
    const eventData = this.event();
    if (!eventData) return;

    this.actionLoading.set(true);
    this.eventService.deleteEvent(eventData.id).subscribe({
      next: () => {
        this.successMessage.set('Event deleted successfully');
        setTimeout(() => {
          this.router.navigate(['/events']);
        }, 1500)
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to delete event');
        this.actionLoading.set(false);
        this.showDeleteModal.set(false);
        setTimeout(() => this.error.set(''), 5000);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }
}
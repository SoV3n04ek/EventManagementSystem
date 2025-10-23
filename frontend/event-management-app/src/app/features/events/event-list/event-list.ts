import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventCardComponent } from '../../../shared/components/event-card/event-card';
import { EventListItem } from '../../../models/event';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, FormsModule, EventCardComponent],
  templateUrl: './event-list.html',
  styleUrl: './event-list.css'
})
export class EventListComponent implements OnInit {
  events = signal<EventListItem[]>([]);
  loading = signal(true);
  error = signal('');
  searchQuery = signal('');
  successMessage = signal('');

  filteredEvents = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return this.events();

    return this.events().filter(event => 
      event.name.toLowerCase().includes(query) ||
      event.location.toLowerCase().includes(query) ||
      event.shortDescription.toLowerCase().includes(query)
    );
  });

  isAuthenticated = computed(() => !!this.authService.currentUser());

  constructor(
    private eventService: EventService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading.set(true);
    this.error.set('');

    this.eventService.getPublicEvents().subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load events. Please try again later.');
        this.loading.set(false);
        console.error('Error loading events:', err);
      }
    });
  }

  onSearchChange(): void {
    // Debounce is handled by signal computed
  }

  handleJoinEvent(eventId: number): void {
    this.eventService.joinEvent(eventId).subscribe({
      next: () => {
        this.successMessage.set('Successfully joined the event!');
        this.loadEvents(); // Refresh to update participant count
        setTimeout(() => this.successMessage.set(''), 3000);
      },
      error: (err) => {
        const message = err.error?.message || err.error?.error || 'Failed to join event';
        this.error.set(message);
        setTimeout(() => this.error.set(''), 5000);
      }
    });
  }
}
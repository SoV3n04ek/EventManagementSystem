import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { BehaviorSubject, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-event-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './event-create.html',
  styleUrl: './event-create.css'
})
export class EventCreateComponent implements OnInit, OnDestroy {
  eventForm: FormGroup;
  isEditMode = false;
  eventId: number | null = null;

  private loadingSubject = new BehaviorSubject<boolean>(false);
  private errorMessageSubject = new BehaviorSubject<string>('');
  private destroy$ = new Subject<void>();

  loading$ = this.loadingSubject.asObservable();
  errorMessage$ = this.errorMessageSubject.asObservable();

  constructor(
    private fb: FormBuilder,
    private eventService: EventService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.eventForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(255)]],
      description: ['', [Validators.minLength(10), Validators.maxLength(2000)]],
      eventDate: ['', [Validators.required, this.futureDateValidator]],
      location: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(500)]],
      capacity: [null, [Validators.min(1)]],
      isPublic: [true, [Validators.required]]
    });
  }

  ngOnInit(): void {
    // Check if we're in edit mode
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.eventId = params['id'] ? Number(params['id']) : null;
      this.isEditMode = !!this.eventId;

      if (this.isEditMode && this.eventId) {
        this.loadEventForEditing(this.eventId);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadEventForEditing(eventId: number): void {
    this.loadingSubject.next(true);

    this.eventService.getEventById(eventId).subscribe({
      next: (event) => {
        // Pre-populate form with existing event data
        this.eventForm.patchValue({
          name: event.name,
          description: event.description || '',
          eventDate: this.formatDateForInput(event.eventDate),
          location: event.location,
          capacity: event.capacity,
          isPublic: event.isPublic
        });
        this.loadingSubject.next(false);
      },
      error: (err) => {
        this.errorMessageSubject.next('Failed to load event for editing');
        this.loadingSubject.next(false);
      }
    });
  }

  private formatDateForInput(dateString: string | Date): string {
    const date = new Date(dateString);
    return date.toISOString().slice(0, 16); // Format: YYYY-MM-DDTHH:mm
  }

  getMinDateTime(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  futureDateValidator(control: any) {
    if (!control.value) return null;

    const selectedDate = new Date(control.value);
    const now = new Date();

    if (selectedDate <= now) {
      return { futureDate: true };
    }

    return null;
  }

  onSubmit(): void {
    if (this.eventForm.invalid) {
      this.eventForm.markAllAsTouched();
      return;
    }

    this.loadingSubject.next(true);
    this.errorMessageSubject.next('');

    const formValue = this.eventForm.value;

    const eventData = {
      ...formValue,
      eventDate: new Date(formValue.eventDate).toISOString(),
      capacity: formValue.capacity || null
    };

    if (this.isEditMode && this.eventId) {
      // Update existing event
      this.eventService.updateEvent(this.eventId, eventData).subscribe({
        next: (response) => {
          this.router.navigate(['/events', this.eventId]);
        },
        error: (err) => {
          this.handleError(err);
        }
      });
    } else {
      // Create new event
      this.eventService.createEvent(eventData).subscribe({
        next: (response) => {
          this.router.navigate(['/events', response.id]);
        },
        error: (err) => {
          this.handleError(err);
        }
      });
    }
  }

  private handleError(err: any): void {
    this.errorMessageSubject.next(
      err.error?.message ||
      err.error?.error ||
      `Failed to ${this.isEditMode ? 'update' : 'create'} event. Please try again.`
    );
    this.loadingSubject.next(false);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  goBack(): void {
    if (this.isEditMode && this.eventId) {
      this.router.navigate(['/events', this.eventId]);
    } else {
      this.router.navigate(['/events']);
    }
  }

  // Helper method for template
  get submitButtonText(): string {
    return this.isEditMode ? 'Update Event' : 'Create Event';
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Event' : 'Create New Event';
  }
}
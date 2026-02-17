import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  input,
  effect,
  linkedSignal
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { Router } from '@angular/router';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Event } from '../../../models/event';
import { of } from 'rxjs';

/**
 * EventCreateComponent — Unified Create/Edit State Machine
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. DECLARATIVE DATA FETCHING: rxResource bound to id() input
 * 2. REACTIVE FORM HYDRATION: linkedSignal + effect for form sync
 * 3. SECURITY: Computed isForbidden signal prevents unauthorized edits
 * 4. ZONELESS-READY: No imperative lifecycle logic
 * ─────────────────────────────────────────────────────────────
 */
@Component({
  selector: 'app-event-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './event-create.html',
  styleUrl: './event-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly eventService = inject(EventService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Route-bound inputs via withComponentInputBinding() ──
  readonly id = input<string>();

  // ── DECLARATIVE DATA FETCHING with rxResource (Angular 20.3 API) ──
  readonly eventResource = rxResource<Event | null, { id: string | undefined }>({
    params: () => ({ id: this.id() }),
    stream: ({ params }) => {
      if (!params.id) return of(null);
      return this.eventService.getEventById(Number(params.id));
    }
  });

  // ── REACTIVE FORM HYDRATION with linkedSignal ──
  readonly formData = linkedSignal(() => {
    const event = this.eventResource.value();
    if (!event) {
      return {
        name: '',
        description: '',
        eventDate: '',
        location: '',
        capacity: null as number | null,
        isPublic: true
      };
    }
    return {
      name: event.name,
      description: event.description || '',
      eventDate: this.formatDateForInput(event.eventDate),
      location: event.location,
      capacity: event.capacity ?? null,
      isPublic: event.isPublic
    };
  });

  // ── Sync formData to eventForm (single source of truth) ──
  constructor() {
    effect(() => {
      const data = this.formData();
      this.eventForm.patchValue(data, { emitEvent: false });
    });
  }

  // ── SECURITY: Computed access control ──
  readonly isForbidden = computed(() => {
    const id = this.id();
    const event = this.eventResource.value();
    return !!id && !!event && !event.isOrganizer;
  });

  // ── Derived State (Signals) ──
  readonly isEdit = computed(() => !!this.id());
  readonly loading = computed(() => this.eventResource.isLoading());
  readonly errorMessage = signal('');

  readonly pageTitle = computed(() =>
    this.isEdit() ? 'Edit Event' : 'Create New Event'
  );

  readonly submitButtonText = computed(() =>
    this.isEdit() ? 'Update Event' : 'Create Event'
  );

  // ── Form Configuration ──
  readonly eventForm: FormGroup = this.fb.group({
    name: ['', [
      Validators.required,
      Validators.minLength(3),
      Validators.maxLength(255)
    ]],
    description: ['', [
      Validators.minLength(10),
      Validators.maxLength(2000)
    ]],
    eventDate: ['', [
      Validators.required,
      this.futureDateValidator.bind(this)
    ]],
    location: ['', [
      Validators.required,
      Validators.minLength(2),
      Validators.maxLength(500)
    ]],
    capacity: [null, [Validators.min(1)]],
    isPublic: [true, [Validators.required]]
  });

  /**
   * Date validator: Ensures event date is in the future
   */
  private futureDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    const inputDate = new Date(control.value);
    const now = new Date();
    return inputDate > now ? null : { futureDate: 'Event date must be in the future' };
  }

  /**
   * Format ISO date to datetime-local input format
   */
  private formatDateForInput(dateValue: Date | string): string {
    const date = new Date(dateValue);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  /**
   * Get minimum datetime for input validation
   */
  getMinDateTime(): string {
    const now = new Date();
    return this.formatDateForInput(now);
  }

  /**
   * Form submission handler
   */
  onSubmit(): void {
    if (this.eventForm.invalid) {
      this.eventForm.markAllAsTouched();
      return;
    }

    if (this.isForbidden()) {
      this.toastService.show('You are not authorized to edit this event', 'error');
      return;
    }

    const formValue = this.eventForm.value;
    const operation$ = this.isEdit()
      ? this.eventService.updateEvent(Number(this.id()), formValue)
      : this.eventService.createEvent(formValue);

    operation$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const eventId = this.isEdit() ? Number(this.id()) : (result as any).id;
          this.toastService.show(
            this.isEdit() ? 'Event updated successfully!' : 'Event created successfully!',
            'success'
          );
          this.router.navigate(['/events', eventId]);
        },
        error: (err) => {
          console.error('Error saving event:', err);
          this.errorMessage.set(
            err?.error?.message || 'Failed to save event. Please try again.'
          );
          this.toastService.show('Failed to save event', 'error');
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }
}
import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  input,
  OnInit
} from '@angular/core';
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

/**
 * EventCreateComponent — Create / Edit Event
 *
 * Architecture:
 * ─────────────────────────────────────────────────────────────
 * 1. Route data `isEdit` bound via input() + withComponentInputBinding()
 * 2. Route param `id` bound via input() — no ActivatedRoute.
 * 3. takeUntilDestroyed() replaces Subject + ngOnDestroy pattern.
 * 4. linkedSignal-like computed for isEditMode derived from
 *    route data, but writable signals for loading/error states.
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
export class EventCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly eventService = inject(EventService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Route-bound inputs via withComponentInputBinding() ──
  readonly id = input<string>();
  readonly isEdit = input<boolean>(false);

  // ── Reactive State (signals) ──
  readonly loading = signal(false);
  readonly errorMessage = signal('');

  // ── Derived ──
  readonly pageTitle = computed(() =>
    this.isEdit() ? 'Edit Event' : 'Create New Event'
  );

  readonly submitButtonText = computed(() =>
    this.isEdit() ? 'Update Event' : 'Create Event'
  );

  readonly eventForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.minLength(10)]],
    eventDate: ['', [Validators.required, this.futureDateValidator]],
    location: ['', [Validators.required]],
    capacity: [null, [Validators.min(1)]],
    isPublic: [true, [Validators.required]]
  });

  ngOnInit(): void {
    if (this.isEdit() && this.id()) {
      this.loadEventForEditing(Number(this.id()));
    }
  }

  private loadEventForEditing(eventId: number): void {
    this.loading.set(true);

    this.eventService.getEventById(eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (event) => {
          this.eventForm.patchValue({
            name: event.name,
            description: event.description || '',
            eventDate: this.formatDateForInput(event.eventDate),
            location: event.location,
            capacity: event.capacity,
            isPublic: event.isPublic
          });
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error loading event:', err);
          this.errorMessage.set('Failed to load event for editing');
          this.loading.set(false);
        }
      });
  }

  onSubmit(): void {
    if (this.eventForm.invalid) {
      this.eventForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const formValue = this.eventForm.value;
    const eventData = {
      ...formValue,
      eventDate: new Date(formValue.eventDate).toISOString(),
      capacity: formValue.capacity || null
    };

    const operation$ = this.isEdit()
      ? this.eventService.updateEvent(Number(this.id()), eventData)
      : this.eventService.createEvent(eventData);

    operation$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (this.isEdit()) {
            this.router.navigate(['/events', this.id()]);
          } else {
            this.router.navigate(['/events', (result as any).id]);
          }
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message || err.error?.error || 'Failed to save event'
          );
          this.loading.set(false);
        }
      });
  }

  goBack(): void {
    if (this.isEdit() && this.id()) {
      this.router.navigate(['/events', this.id()]);
    } else {
      this.router.navigate(['/events']);
    }
  }

  getMinDateTime(): string {
    return new Date().toISOString().slice(0, 16);
  }

  private futureDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    return new Date(control.value) > new Date() ? null : { futureDate: true };
  }

  private formatDateForInput(dateStr: string | Date): string {
    const date = new Date(dateStr);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service'

@Component({
  selector: 'app-event-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './event-create.html',
  styleUrl: './event-create.css'
})
export class EventCreateComponent {
  eventForm: FormGroup;
  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private eventService: EventService,
    private router: Router
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

  futureDateValidator(control: any) {
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

    this.loading.set(true);
    this.errorMessage.set('');

    const formValue = this.eventForm.value;

    const eventData = {
      ...formValue,
      eventDate: new Date(formValue.eventDate).toISOString(),
      capacity: formValue.capacity || null
    };

    this.eventService.createEvent(eventData).subscribe({
      next: (response) => {
        this.router.navigate(['/events', response.id]);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message ||
          err.error?.error ||
          'Failed to create event. Please try again.'
        );
        this.loading.set(false);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }
}

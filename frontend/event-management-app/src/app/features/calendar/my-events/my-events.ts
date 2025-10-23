import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { CalendarViewDto, CalendarEventDto } from '../../../models/event';

@Component({
  selector: 'app-my-events',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-events.html',
  styleUrl: './my-events.css'
})
export class MyEventsComponent implements OnInit {
  calendarData = signal<CalendarViewDto | null>(null);
  loading = signal(true);
  error = signal('');
  currentDate = signal(new Date());
  viewType = signal<'month' | 'week'>('month');

  weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  currentMonthYear = computed(() => {
    const date = this.currentDate();
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  calendarDays = computed(() => {
    const date = this.currentDate();
    const year = date.getFullYear();
    const month = date.getMonth();

    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startingDayOfWeek = firstDay.getDay();

    const days: Array<{
      date: Date;
      isCurrentMonth: boolean;
      isToday: boolean;
      events: CalendarEventDto[];
    }> = [];

    // Previous month days
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const day = prevMonthLastDay - i;
      const dayDate = new Date(year, month - 1, day);
      days.push({
        date: dayDate,
        isCurrentMonth: false,
        isToday: this.isSameDay(dayDate, new Date()),
        events: this.getEventsForDay(dayDate)
      });
    }

    // Current month days
    for (let day = 1; day <= daysInMonth; day++) {
      const dayDate = new Date(year, month, day);
      days.push({
        date: dayDate,
        isCurrentMonth: true,
        isToday: this.isSameDay(dayDate, new Date()),
        events: this.getEventsForDay(dayDate)
      });
    }

    // Next month days
    const remainingDays = 42 - days.length;
    for (let day = 1; day <= remainingDays; day++) {
      const dayDate = new Date(year, month + 1, day);
      days.push({
        date: dayDate,
        isCurrentMonth: false,
        isToday: this.isSameDay(dayDate, new Date()),
        events: this.getEventsForDay(dayDate)
      });
    }

    return days;
  });

  constructor(private eventService: EventService) {}

  ngOnInit(): void {
    this.loadCalendarEvents();
  }

  loadCalendarEvents(): void {
    this.loading.set(true);
    this.error.set('');

    const date = this.currentDate();
    const startDate = this.getStartDate(date);
    const endDate = this.getEndDate(date);

    this.eventService.getCalendarEvents(startDate, endDate, this.viewType()).subscribe({
      next: (data) => {
        this.calendarData.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load calendar events');
        this.loading.set(false);
        console.error('Error loading calendar', err);
      }
    });
  }

  getStartDate(date: Date): Date {
    if (this.viewType() === 'week') {
      const day = date.getDay();
      const diff = date.getDate() - day;
      return new Date(date.getFullYear(), date.getMonth(), diff);
    }
    return new Date(date.getFullYear(), date.getMonth() - 1, 1);
  }

  
  getEndDate(date: Date): Date {
    if (this.viewType() === 'week') {
      const day = date.getDay();
      const diff = date.getDate() + (6 - day);
      return new Date(date.getFullYear(), date.getMonth(), diff, 23, 59, 59);
    }
    // Month view: get last day of next month
    return new Date(date.getFullYear(), date.getMonth() + 2, 0, 23, 59, 59);
  }

  getEventsForDay(date: Date): CalendarEventDto[] {
    const events = this.calendarData()?.events || [];
    return events.filter(event => 
      this.isSameDay(new Date(event.start), date)
    );
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getFullYear() === date2.getFullYear() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getDate() === date2.getDate();
  }

  isToday(date: Date): boolean {
    return this.isSameDay(date, new Date());
  }

  getWeekDay(index: number): Date {
    const current = this.currentDate();
    const day = current.getDay();
    const diff = current.getDate() - day + index;
    return new Date(current.getFullYear(), current.getMonth(), diff);
  }

  previousMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
    this.loadCalendarEvents();
  }

  nextMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
    this.loadCalendarEvents();
  }

  goToToday(): void {
    this.currentDate.set(new Date());
    this.loadCalendarEvents();
  }

  setViewType(type: 'month' | 'week'): void {
    this.viewType.set(type);
    this.loadCalendarEvents();
  }
}
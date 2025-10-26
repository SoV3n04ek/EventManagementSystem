import { Component, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { CalendarViewDto, CalendarEventDto } from '../../../models/event';
import { 
  Observable, 
  BehaviorSubject, 
  combineLatest, 
  map, 
  shareReplay, 
  catchError,
  switchMap,
  of 
} from 'rxjs';

@Component({
  selector: 'app-my-events',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-events.html',
  styleUrl: './my-events.css'
})
export class MyEventsComponent implements OnInit {
  private currentDateSubject = new BehaviorSubject<Date>(new Date());
  private viewTypeSubject = new BehaviorSubject<'month' | 'week'>('month');
  
  currentDate$ = this.currentDateSubject.asObservable();
  viewType$ = this.viewTypeSubject.asObservable();
  
  calendarData$: Observable<CalendarViewDto | null> = combineLatest([
    this.currentDate$,
    this.viewType$
  ]).pipe(
    map(([date, viewType]) => {
      // Calculate date range based on current date and view type
      const startDate = this.getStartDate(date, viewType);
      const endDate = this.getEndDate(date, viewType);
      return { startDate, endDate, viewType };
    }),
    switchMap(params => 
      // Automatically fetch new data when date or viewType changes
      this.eventService.getCalendarEvents(
        params.startDate, 
        params.endDate,  
        params.viewType
      ).pipe(
        // Handle errors gracefully
        catchError(error => {
          console.error('Error loading calendar:', error);
          return of(null);
        })
      )
    ),
    shareReplay(1)
  );
 
  loading$ = new BehaviorSubject<boolean>(true);
  error$ = new BehaviorSubject<string>('');
  
  constructor(private eventService: EventService) {}
  
  ngOnInit(): void {
    
  } 

  weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  currentMonthYear = computed(() => {
    const date = this.currentDateSubject.value;
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  nextPeriod(): void {
    const current = this.currentDateSubject.value;
    const viewType = this.viewTypeSubject.value;
    if (viewType === 'week') {
      // Go forward one week
      this.currentDateSubject.next(
        new Date(current.getFullYear(), current.getMonth(), current.getDate() + 7));
    } else {
      // Go forward one month
      this.currentDateSubject.next(
        new Date(current.getFullYear(), current.getMonth() + 1, 1));
    }
  }
  
  previousPeriod(): void {
    const current = this.currentDateSubject.value;
    const viewType = this.viewTypeSubject.value;
    
    if (viewType === 'week') {
      // Go back one week
      this.currentDateSubject.next(
        new Date(current.getFullYear(), current.getMonth(), current.getDate() - 7));
    } else {
      // Go back one month
      this.currentDateSubject.next(
        new Date(current.getFullYear(), current.getMonth() - 1, 1));
    }
  }
  
  goToToday(): void {
    this.currentDateSubject.next(new Date());
  }

  setViewType(type: 'month' | 'week'): void {
    this.viewTypeSubject.next(type);
  }

  getStartDate(date: Date, viewType: string): Date {
    if (viewType === 'week') {
      const day = date.getDay();
      const diff = date.getDate() - day;
      return new Date(date.getFullYear(), date.getMonth(), diff);
    }
    return new Date(date.getFullYear(), date.getMonth() - 1, 1);
  }

  getEndDate(date: Date, viewType: string): Date {
    if (viewType === 'week') {
      const day = date.getDay();
      const diff = date.getDate() + (6 - day);
      return new Date(date.getFullYear(), date.getMonth(), diff, 23, 59, 59);
    }
    // Month view: get last day of next month
    return new Date(date.getFullYear(), date.getMonth() + 2, 0, 23, 59, 59);
  }

  getEventsForDay(date: Date, calendarData: CalendarViewDto | null): CalendarEventDto[] {
    const events = calendarData?.events || [];
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
    const current = this.currentDateSubject.value;
    const day = current.getDay();
    const diff = current.getDate() - day + index;
    return new Date(current.getFullYear(), current.getMonth(), diff);
  }

  getCalendarDays(calendarData: CalendarViewDto | null): Array<{
    date: Date;
    isCurrentMonth: boolean;
    isToday: boolean;
    events: CalendarEventDto[];
  }> {
    const date = this.currentDateSubject.value;
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
        events: this.getEventsForDay(dayDate, calendarData)
      });
    }

    // Current month days
    for (let day = 1; day <= daysInMonth; day++) {
      const dayDate = new Date(year, month, day);
      days.push({
        date: dayDate,
        isCurrentMonth: true,
        isToday: this.isSameDay(dayDate, new Date()),
        events: this.getEventsForDay(dayDate, calendarData)
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
        events: this.getEventsForDay(dayDate, calendarData)
      });
    }

    return days;
  }
}
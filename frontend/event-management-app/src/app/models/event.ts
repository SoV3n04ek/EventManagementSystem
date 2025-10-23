export interface Event {
    id: number;
    name: string;
    description?: string;
    eventDate: Date;
    location: string;
    capacity?: number;
    isPublic: boolean;
    organizerName: string;
    participantCount: number;
    participants: Participant[];
}

export interface EventListItem {
    id: number;
    name: string;
    shortDescription: string;
    eventDate: Date;
    location: string;
    participantCount: number;
    isFull: boolean;
}

export interface CreateEventRequest {
    name: string;
    description?: string;
    eventDate: Date; // ISO string
    location: string;
    capacity?: number;
    isPublic: boolean;
}

export interface UpdateEventRequest {
    name?: string;
    description?: string;
    eventDate?: Date; // ISO string
    location?: string;
    capacity?: number;
    isPublic?: boolean;
}

export interface Participant {
    id: number;
    name: string;
}

export interface CalendarEventDto {
    id: number;
    title: string;
    start: Date;
    end: Date;
    location: string;
    isOrganizer: boolean;
}

export interface CalendarViewDto {
  events: CalendarEventDto[];
  startDate: Date;
  endDate: Date;
  viewType: string;
}

export interface CalendarEvent {
    id: number;
    title: string;
    start: Date;
    end: Date;
    location: string;
    description?: string;
    isPublic: boolean;
    participantCount: number;
    capacity?: number;
    isFull: boolean;
    color?: string;
}
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventListItem } from '../../../models/event';
import { RouterLink } from '@angular/router';
  
@Component({
  selector: 'app-event-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './event-card.html',
  styleUrl: './event-card.css'
})
export class EventCardComponent {
  @Input({ required: true }) event!: EventListItem;
  @Input() showJoinButton = false;
  @Input() showLeaveButton = false;
  
  @Output() join = new EventEmitter<number>();
  @Output() leave = new EventEmitter<number>();

  onJoin(): void {
    this.join.emit(this.event.id);
  }

  onLeave(): void {
    this.leave.emit(this.event.id);
  }
}
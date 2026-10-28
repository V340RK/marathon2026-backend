import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { TimeSlotComponent } from '../time-slot/time-slot.component';
import { DatePipe } from '@angular/common';
import { TimeRange } from '@app/models/tables/time-range.model';
import { Table } from '@app/models/tables/table.model';

@Component({
  selector: 'app-time-slots-modal',
  imports: [TimeSlotComponent, DatePipe],
  templateUrl: './time-slots-modal.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './time-slots-modal.component.scss',
})
export class TimeSlotsModalComponent {
  @Input() locationName!: string;
  @Input() table!: Table;
  @Input() date!: string;

  @Output() closeModal = new EventEmitter<void>();
  @Output() timeSlotSelected = new EventEmitter<{ table: Table; timeRange: TimeRange }>();

  onClose() {
    this.closeModal.emit();
  }

  onTimeSlotSelected(timeSlot: TimeRange) {
    this.timeSlotSelected.emit({ table: this.table, timeRange: timeSlot });
  }
}

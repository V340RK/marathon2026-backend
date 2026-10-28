import {
  Component,
  EventEmitter,
  Output,
  ChangeDetectionStrategy,
  computed,
  input,
} from '@angular/core';
import { TimeSlotComponent } from '../time-slot/time-slot.component';
import { Table } from '@app/models/tables/table.model';
import { TimeRange } from '@app/models/tables/time-range.model';
import { HourSlot } from './models';
import { formatTime } from '@app/shared/helpers/date-helpers';

const FIRST_SLOT_MINUTES = 10 * 60;
const LAST_SLOT_MINUTES = 21 * 60 + 30;
const SLOT_STEP_MINUTES = 30;

@Component({
  selector: 'app-table-card',
  imports: [TimeSlotComponent],
  templateUrl: './table-card.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './table-card.component.scss',
})
export class TableCardComponent {
  readonly location = input.required<string>();
  readonly date = input.required<string>();
  readonly table = input.required<Table>();
  // @Output() showAllClicked = new EventEmitter<Table>();
  @Output() timeSlotSelected = new EventEmitter<TimeRange>();

  protected readonly hourSlots = computed<HourSlot[]>(() => this.buildHourSlots());

  onTimeSlotSelected(slot: HourSlot): void {
    if (slot.timeRange) {
      this.timeSlotSelected.emit(slot.timeRange);
    }
  }

  // onShowAllClick(): void {
  //   this.showAllClicked.emit(this.table);
  // }

  private buildHourSlots(): HourSlot[] {
    const timeRanges = this.table().timeRanges ?? [];
    const slots: HourSlot[] = [];

    for (
      let slotStart = FIRST_SLOT_MINUTES;
      slotStart <= LAST_SLOT_MINUTES;
      slotStart += SLOT_STEP_MINUTES
    ) {
      const timeRange = timeRanges.find((range) => {
        const start = new Date(range.start);
        const minutes = start.getHours() * 60 + start.getMinutes();
        return minutes >= slotStart && minutes < slotStart + SLOT_STEP_MINUTES;
      });
      const hour = Math.floor(slotStart / 60);
      const minute = (slotStart % 60).toString().padStart(2, '0');
      const bucketLabel = `${hour}:${minute}`;
      slots.push({
        label: timeRange ? formatTime(timeRange.start) : bucketLabel,
        timeRange: timeRange ?? null,
      });
    }

    return slots;
  }
}

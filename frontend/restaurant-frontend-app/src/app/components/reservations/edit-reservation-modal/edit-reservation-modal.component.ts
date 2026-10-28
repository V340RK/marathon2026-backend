import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { Table } from '@app/models/tables/table.model';
import { TimeRange } from '@app/models/tables/time-range.model';
import { SelectOption } from '@app/models/shared/select-option.model';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { CustomNumberComponent } from '@app/shared/custom-components/custom-number/custom-number.component';
import {
  addMinutesIso,
  findClosestDuration,
  formatTime,
  minutesBetween,
} from '@app/shared/helpers/date-helpers';
import { STAY_DURATION_OPTIONS } from './constants';
import { ReservationDraftChanges } from './models';

@Component({
  selector: 'app-edit-reservation-modal',
  imports: [DatePipe, ModalComponent, CustomSelectComponent, CustomNumberComponent],
  templateUrl: './edit-reservation-modal.component.html',
  styleUrl: './edit-reservation-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class EditReservationModalComponent {
  readonly locationName = input.required<string>();
  readonly date = input.required<string>();
  readonly table = input.required<Table>();
  readonly timeRange = input.required<TimeRange>();
  readonly guests = input.required<number>();

  readonly closed = output<void>();
  readonly saveChanges = output<ReservationDraftChanges>();

  protected readonly durationOptions = STAY_DURATION_OPTIONS;
  private readonly durationValues = STAY_DURATION_OPTIONS.map((option) => Number(option.value));

  protected readonly selectedStart = linkedSignal(() => this.timeRange().start);
  protected readonly selectedGuests = linkedSignal(() => this.guests());
  protected readonly selectedDuration = linkedSignal(() =>
    findClosestDuration(
      minutesBetween(this.timeRange().start, this.timeRange().end),
      this.durationValues
    )
  );

  protected readonly timeOptions = computed<SelectOption[]>(() => {
    const starts = this.table().timeRanges.map((range) => range.start);
    const current = this.timeRange().start;
    const allStarts = starts.includes(current) ? starts : [current, ...starts];

    return allStarts.map((start) => ({ text: formatTime(start), value: start }));
  });

  protected readonly selectedTimeOption = computed(() =>
    this.timeOptions().find((option) => option.value === this.selectedStart())
  );

  protected readonly selectedDurationOption = computed(() =>
    this.durationOptions.find((option) => Number(option.value) === this.selectedDuration())
  );

  onClose(): void {
    this.closed.emit();
  }

  onStartSelected(start: string): void {
    this.selectedStart.set(start);
  }

  onGuestsChange(guests: number | undefined): void {
    if (guests !== undefined) {
      this.selectedGuests.set(guests);
    }
  }

  onDurationSelected(minutes: string): void {
    this.selectedDuration.set(Number(minutes));
  }

  onSave(): void {
    const start = this.selectedStart();
    this.saveChanges.emit({
      timeRange: { start, end: addMinutesIso(start, this.selectedDuration()) },
      guests: this.selectedGuests(),
    });
  }
}

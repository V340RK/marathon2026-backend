import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TimeRange } from '@app/models/tables/time-range.model';
import { formatTime } from '@app/shared/helpers/date-helpers';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

@Component({
  selector: 'app-reservation-review-modal',
  imports: [DatePipe, ModalComponent],
  templateUrl: './reservation-review-modal.component.html',
  styleUrl: './reservation-review-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ReservationReviewModalComponent {
  readonly locationName = input.required<string>();
  readonly date = input.required<string>();
  readonly timeRange = input.required<TimeRange>();
  readonly guests = input.required<number>();
  readonly submitting = input(false);

  readonly closed = output<void>();
  readonly edit = output<void>();
  readonly confirmReservation = output<void>();

  readonly timeLabel = computed(() => {
    const range = this.timeRange();
    return `${formatTime(range.start)} - ${formatTime(range.end)}`;
  });

  readonly guestsLabel = computed(() => {
    const count = this.guests();
    return `${count} ${count === 1 ? 'person' : 'people'}`;
  });

  onClose(): void {
    this.closed.emit();
  }

  onEdit(): void {
    this.edit.emit();
  }

  onConfirm(): void {
    this.confirmReservation.emit();
  }
}

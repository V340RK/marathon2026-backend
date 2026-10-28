import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
} from '@angular/core';

import {
  Reservation,
  ReservationStatus,
} from '@app/models/reservations/reservation.model';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';

@Component({
  selector: 'app-reservation-confirmed',
  standalone: true,
  imports: [
    DatePipe,
    CustomButtonComponent,
  ],
  templateUrl: './reservation-confirmed.component.html',
  styleUrl: './reservation-confirmed.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.fade-out]': 'isFading()',
  },
})
export class ReservationConfirmedComponent {
  readonly reservation = input.required<Reservation>();
  readonly customClass = input('');

  readonly closed = output<void>();
  readonly editReservation = output<void>();
  readonly cancelReservation = output<void>();

  readonly isFading = signal(false);

  protected readonly ReservationStatus = ReservationStatus;

  onClose(): void {
    if (this.isFading()) {
      return;
    }

    this.isFading.set(true);
    this.closed.emit();
  }

  onEdit(): void {
    this.editReservation.emit();
  }

  onCancel(): void {
    this.cancelReservation.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}

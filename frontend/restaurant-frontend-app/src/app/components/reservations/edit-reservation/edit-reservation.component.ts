import { Component, EventEmitter, input } from '@angular/core';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';
import { DatePipe } from '@angular/common';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { CustomNumberComponent } from '@app/shared/custom-components/custom-number/custom-number.component';

@Component({
  selector: 'app-edit-reservation',
  imports: [DatePipe, CustomSelectComponent, CustomButtonComponent, CustomNumberComponent],
  templateUrl: './edit-reservation.component.html',
  styleUrl: './edit-reservation.component.scss',
})
export class EditReservationComponent {
  private defaultReservation: Reservation = {
    id: '',
    tableId: '',
    customerId: '',
    startTime: new Date(),
    endTime: new Date(),
    guestCount: 6,
    status: ReservationStatus.Reserved,
    locationAddress: '1 Test Street',
    tableCapacity: 10,
  };
  reservation = input<Reservation>(this.defaultReservation);
  backdropClick = new EventEmitter<void>();

  timeOptions: string[];
  selectedStartTime: string =
    this.defaultReservation.startTime.getHours().toString().padStart(2, '0') +
    ':' +
    this.defaultReservation.startTime.getMinutes().toString().padStart(2, '0');
  selectedEndTime: string =
    this.defaultReservation.endTime.getHours().toString().padStart(2, '0') +
    ':' +
    this.defaultReservation.endTime.getMinutes().toString().padStart(2, '0');
  selectedGuestCount: number = this.defaultReservation.guestCount;

  constructor() {
    this.timeOptions = this.generateTimeOptions();
  }

  setStartTime(time: string): void {
    this.selectedStartTime = time;
  }

  setEndTime(time: string): void {
    this.selectedEndTime = time;
  }

  submit() {
    console.log('mock submit');
    // Here you would typically handle the form submission,
    this.backdropClick.emit();
  }

  onBackdropClick() {
    this.backdropClick.emit();
  }

  guestCountChanged(newCount: number) {
    this.selectedGuestCount = newCount;
  }

  private generateTimeOptions() {
    const startHour = 10;
    const endHour = 22;
    const maxMinutes = (endHour - startHour) * 60;
    const timeOptions: string[] = [];
    for (let i = 0; i <= maxMinutes; i += 15) {
      const hour = Math.floor((startHour * 60 + i) / 60);
      const minute = (startHour * 60 + i) % 60;
      const formattedTime = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
      timeOptions.push(formattedTime);
    }

    return timeOptions;
  }
}

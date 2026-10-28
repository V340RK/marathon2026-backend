import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { HttpClient, HttpContext } from '@angular/common/http';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { Observable } from 'rxjs';
import { TimeRange } from '@app/models/tables/time-range.model';
import {
  Reservation,
  ReservationCustomer,
  MakeReservationRequestBody,
  CancelReservationResponse
} from '@app/models/reservations/reservation.model';
import { FeedbackReservationResponseItem } from '@app/models/feedbacks/feedback-reservation-response-item.model';

@Injectable({
  providedIn: 'root',
})
export class ReservationService {
  private readonly baseUrl = inject(API_BASE_URL);
  private http: HttpClient = inject(HttpClient);

  // makeReservation$(tableId: string, timeRange: TimeRange, guests: number): Observable<Reservation> {
  //   const url = `${this.baseUrl}/reservations`;
  //   const body = {
  //     tableId: tableId,
  //     startTime: timeRange.start,
  //     endTime: timeRange.end,
  //     guestCount: guests,
  //   };

  //   return this.http.post<Reservation>(url, body);
  // }

  makeReservation$(
    tableId: string,
    timeRange: TimeRange,
    guests: number,
    customerEmail?: string | null,
    skipErrorToast = false
  ): Observable<Reservation> {
    const url = `${this.baseUrl}/reservations`;
    const body: MakeReservationRequestBody = {
      tableId: tableId,
      startTime: timeRange.start,
      endTime: timeRange.end,
      guestCount: guests,
    };
    console.log('[SEND] POST body:', body);

    if (customerEmail != null) {
      body.customerEmail = customerEmail;
    }

    return this.http.post<Reservation>(url, body, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, skipErrorToast),
    });
  }

  editReservation$(
    reservationId: string,
    timeRange: TimeRange,
    guestCount: number
  ): Observable<Reservation> {
    const url = `${this.baseUrl}/reservations`;
    const body = {
      reservationId,
      guestCount,
      startTime: timeRange.start,
      endTime: timeRange.end,
    };

    return this.http.put<Reservation>(url, body);
  }

  cancelReservation$(reservationId: string): Observable<CancelReservationResponse> {
    const url = `${this.baseUrl}/reservations`;

    return this.http.delete<CancelReservationResponse>(url, { body: { id: reservationId } });
  }

  getReservationsForUser$(): Observable<Reservation[]> {
    const url = `${this.baseUrl}/reservations`;
    return this.http.get<Reservation[]>(url);
  }

  getReservationFeedback(
    reservationId: string
  ): Observable<FeedbackReservationResponseItem | null> {
    return this.http.get<FeedbackReservationResponseItem>(
      `${this.baseUrl}/reservations/${reservationId}/feedback`
    );
  }

  getReservationById(reservationId: string): Observable<ReservationCustomer> {
    return this.http.get<ReservationCustomer>(`${this.baseUrl}/reservations/${reservationId}`);
  }

  changeReservationStatus$(id: string, newStatus: number): Observable<ReservationCustomer> {
    const url = `${this.baseUrl}/reservations/${id}/status`;
    const body = { newStatus };
    return this.http.patch<ReservationCustomer>(url, body);
  }
}

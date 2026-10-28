import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReviewSort } from '@app/models/enums/review-sort.model';
import { ReviewType } from '@app/models/enums/review-type.model';
import { CreateFeedbackRequest } from '@app/models/feedbacks/create-feedback-request.model';
import { UpdateFeedbackRequest } from '@app/models/feedbacks/update-feedback-request.model';
import { FeedbackResponse } from '@app/models/feedbacks/feedback-response.model';
import { FeedbackResponseItem } from '@app/models/feedbacks/feedback-response-item.model';
import { FeedbackReservationResponseItem } from '@app/models/feedbacks/feedback-reservation-response-item.model';

@Injectable({
  providedIn: 'root',
})
export class FeedbacksService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  leaveFeedback(feedback: CreateFeedbackRequest, token: string): Observable<FeedbackResponseItem> {
    if (token) {
      feedback.token = token; // Attach the token for anonymous feedback
    }

    return this.http.post<FeedbackResponseItem>(`${this.baseUrl}/feedback`, feedback);
  }

  updateFeedback(updateRequest: UpdateFeedbackRequest): Observable<string> {
    return this.http.put<string>(`${this.baseUrl}/feedback`, updateRequest);
  }

  getLocationFeedbacks(
    locationId: string,
    type: ReviewType,
    sort: ReviewSort,
    page = 1
  ): Observable<FeedbackResponse> {
    const params = new HttpParams()
      .set('locationId', locationId)
      .set('type', type)
      .set('sort', sort)
      .set('page', page.toString());

    return this.http.get<FeedbackResponse>(`${this.baseUrl}/feedback`, { params });
  }

  getFeedbackByReservation(reservationId: string): Observable<FeedbackReservationResponseItem> {
    return this.http.get<FeedbackReservationResponseItem>(
      `${this.baseUrl}/reservations/${reservationId}/feedback`
    );
  }
}

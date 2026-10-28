import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class FeedbackQrService {
  private readonly frontendBaseUrl = window.location.origin + '/main';
  private readonly http: HttpClient = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  generateFeedbackUrl(
    token: string,
    reservationId: string,
    waiterName: string,
    isNewFeedback: boolean
  ): string {
    const params = new URLSearchParams({
      token,
      reservationId,
      waiterName,
      isNewFeedback: String(isNewFeedback),
    });
    const url = `${this.frontendBaseUrl}?${params.toString()}`;

    return url;
  }

  getAnonymousFeedbackToken(reservationId: string): Observable<string> {
    const url = this.baseUrl + '/feedback/token';
    const params = new URLSearchParams({
      reservationId: reservationId,
    });
    return this.http.get<string>(`${url}?${params.toString()}`);
  }
}

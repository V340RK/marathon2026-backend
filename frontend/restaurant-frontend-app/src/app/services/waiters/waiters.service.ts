import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

interface WaiterLocationResponse {
  locationId: string;
}

@Injectable({
  providedIn: 'root',
})
export class WaitersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getWaiterLocationId(waiterId: string): Observable<string> {
    return this.http
      .get<WaiterLocationResponse>(`${this.baseUrl}/waiters/${waiterId}/location`)
      .pipe(map((response) => response.locationId));
  }
}

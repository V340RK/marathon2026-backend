import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { LocationResponse } from '@app/models/locations/location-response.model';

@Injectable({
  providedIn: 'root',
})
export class LocationsService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getLocations(): Observable<LocationResponse[]> {
    return this.http.get<LocationResponse[]>(`${this.baseUrl}/locations`);
  }

  getLocationById(id: string): Observable<LocationResponse> {
    return this.http.get<LocationResponse>(`${this.baseUrl}/locations/${id}`);
  }

  getSpecialtyDishes(id: string): Observable<DishListResponse[]> {
    return this.http.get<DishListResponse[]>(`${this.baseUrl}/locations/${id}/dishes`);
  }
}

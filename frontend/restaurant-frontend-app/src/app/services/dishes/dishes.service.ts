import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class DishesService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getPopularDishes(): Observable<DishListResponse[]> {
    return this.http.get<DishListResponse[]>(`${this.baseUrl}/dishes/popular`);
  }

  // /dishes
  // /dishes?dishType=MainCourse&sortBy=PriceDescending&name=Spaghetti
  getDishes(dishType?: string, sortBy?: string, name?: string): Observable<DishListResponse[]> {
    let params = new HttpParams();

    if (dishType) {
      params = params.set('dishType', dishType);
    }
    if (sortBy) {
      params = params.set('sortBy', sortBy);
    }
    if (name) {
      params = params.set('name', name);
    }

    return this.http.get<DishListResponse[]>(`${this.baseUrl}/dishes`, { params });
  }
}

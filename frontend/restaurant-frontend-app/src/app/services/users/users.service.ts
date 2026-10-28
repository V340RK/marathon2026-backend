import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProfileImageResponse } from '@app/models/profile-img/profile-image-response.model';
import { UserModel } from '@app/auth/models/user.model';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  // GET /user/{id}/profile-image
  getUserProfileImage(userId: string): Observable<ProfileImageResponse> {
    const url = `${this.baseUrl}/users/${userId}/profile-image`;
    return this.http.get<ProfileImageResponse>(url);
  }

  getUsers(fullName: string): Observable<UserModel[]> {
    const url = `${this.baseUrl}/users`;
    const params = { fullName };
    return this.http.get<UserModel[]>(url, { params });
  }

  getUserName(id: string): Observable<string> {
    const url = `${this.baseUrl}/users/${id}/name`;
    return this.http.get<{ name: string }>(url).pipe(map((response) => response.name));
  }
}

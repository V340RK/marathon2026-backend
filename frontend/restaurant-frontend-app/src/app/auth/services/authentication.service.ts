import { inject, Injectable } from '@angular/core';
import { LoginResult } from '../models/login.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { HttpClient } from '@angular/common/http';
import { UserModel } from '../models/user.model';
import { BehaviorSubject, map, Observable } from 'rxjs';
import { Tokens } from '../models/tokens.model';
import { JwtHelperService } from '@auth0/angular-jwt';
@Injectable({
  providedIn: 'root',
})
export class AuthenticationService {
  private readonly baseUrl = inject(API_BASE_URL);
  private user$$ = new BehaviorSubject<UserModel | null>(this.getUser());
  public user$: Observable<UserModel | null> = this.user$$.asObservable();
  private isAuthorised$$ = new BehaviorSubject<boolean>(localStorage.getItem('auth') !== null);
  public isAuthorised$: Observable<boolean> = this.isAuthorised$$.asObservable();
  public jwtHelperService = new JwtHelperService();
  private readonly http = inject(HttpClient);
  
  async signIn(email: string, password: string): Promise<LoginResult> {
    const body = {
      email: email,
      password: password,
    };
    const url = this.baseUrl + '/signin';

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });
    return await this.resolveLoginReponse(response);
  }
  getToken(): string | undefined {
    const authData = localStorage.getItem('auth');
    let parsed;
    let idToken;
    if (authData) {
      parsed = JSON.parse(authData) as Tokens;
      idToken = parsed.idToken;
    }
    return idToken;
  }
  async register(
    firstName: string,
    lastName: string,
    email: string,
    password: string,
    confirmPassword: string
  ) {
    const body = {
      firstName: firstName,
      lastName: lastName,
      email: email,
      password: password,
      confirmPassword: confirmPassword,
    };
    const url = this.baseUrl + '/register';

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });
    return await this.resolveRegisterResponse(response);
  }
  signOut() {
    localStorage.removeItem('auth');
    localStorage.removeItem('user');
    this.user$$.next(null);
    this.isAuthorised$$.next(false);
  }
  getUserNameById$(userId: string): Observable<string> {
    const url = `${this.baseUrl}/users/${userId}/name`;
    return this.http.get<{ name: string }>(url).pipe(map((response) => response.name));
  }
  private async resolveLoginReponse(response: Response): Promise<LoginResult> {
    const result: LoginResult = { success: true };
    try {
      const body = await response.json();
      if (response.ok && body.idToken) {
        const tokens: Tokens = {
          idToken: body.idToken,
          accessToken: body.accessToken,
        };
        localStorage.setItem('auth', JSON.stringify(tokens));
        this.isAuthorised$$.next(true);
        this.loadUser();
      } else if (!response.ok) {
        result.success = false;
        result.message = body.error;
        console.error('Login failed:', result.message);
      } else {
        result.success = false;
        result.message = 'Unexpected response from the server. Please try again later.';
        console.error('Unexpected response:', body);
      }
    } catch (error) {
      result.success = false;
      result.message = 'An error occurred during the login process. Please try again later.';
      console.error('Error processing response:', error);
    }
    return result;
  }
  private async resolveRegisterResponse(response: Response): Promise<LoginResult> {
    const body = await response.json();
    const result: LoginResult = { success: true };
    if (response.ok) {
      return result;
    }
    if (body.error) {
      result.success = false;
      result.message = body.error;
      console.error('Registration failed:', result.message);
    } else {
      result.success = false;
      result.message = 'Unexpected response from the server. Please try again later.';
      console.error('Unexpected response:', body);
    }
    return result;
  }
  private loadUser() {
    const auth = this.getTokens();
    if (auth === null) {
      return;
    }
    if (this.jwtHelperService.isTokenExpired(auth.idToken)) {
      console.warn('ID Token is expired. User data will not be loaded.');
      this.signOut();
      return;
    }
    const url = this.baseUrl + '/me';
    this.http.get<UserModel>(url).subscribe(
      (user) => {
        this.user$$.next(user);
        localStorage.setItem('user', JSON.stringify(user));
      },
      (error) => {
        console.error('Failed to load user data:', error);
        this.user$$.next(null);
      }
    );
  }
  getUser(): UserModel | null {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }
  editProfile(firstName: string, lastName: string): Observable<string> {
    const url = this.baseUrl + '/me';
    const body = { firstName, lastName };
    return new Observable((observer) => {
      this.http.put(url, body).subscribe({
        next: () => {
          this.loadUser();
          observer.next('Profile updated successfully');
          observer.complete();
        },
        error: (error) => {
          console.error('Error updating profile:', error);
          observer.error('Failed to update profile. Please try again later.');
        },
      });
    });
  }
  changePassword(oldPassword: string, newPassword: string): Observable<string> {
    const url = this.baseUrl + '/me/change-password';
    const tokens = this.getTokens();
    if (
      !tokens ||
      !tokens.accessToken ||
      this.jwtHelperService.isTokenExpired(tokens.accessToken)
    ) {
      this.signOut();
      return new Observable((observer) => {
        observer.error('User is not authenticated. Please log in again.');
      });
    }
    const body = {
      oldPassword,
      newPassword,
      accessToken: tokens.accessToken,
    };
    return new Observable((observer) => {
      this.http.put(url, body).subscribe({
        next: () => {
          observer.next('Password changed successfully');
          observer.complete();
        },
        error: (error) => {
          console.error('Error changing password:', error);
          observer.error('Failed to change password. Please try again later.');
        },
      });
    });
  }
  getTokens(): Tokens | null {
    const authStr = localStorage.getItem('auth');
    const auth = authStr ? JSON.parse(authStr) : null;
    const tokens = auth as Tokens;
    if (!tokens || !tokens.idToken || !tokens.accessToken) {
      return null;
    }
    if (this.jwtHelperService.isTokenExpired(tokens.idToken)) {
      console.warn('ID Token is expired. User data will not be loaded.');
      this.signOut();
      return null;
    }
    return tokens;
  }
  // PUT /me/profile-image
  uploadMyProfileImage(base64Image: string): Observable<string> {
    const url = this.baseUrl + '/me/profile-image';
    const body = {
      base64Image: base64Image,
    };
    return this.http.put<string>(url, body);
  }
}

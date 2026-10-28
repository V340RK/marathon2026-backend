import { inject, Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthenticationService } from '../services/authentication.service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  private readonly authService = inject(AuthenticationService);
  private readonly router = inject(Router);

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean | UrlTree> {
    const expectedAuthorization = route.data['authorized'] as boolean;

    return this.authService.isAuthorised$.pipe(
      map((isAuthorized) => {
        if (isAuthorized === expectedAuthorization) return true;
        
        if (!isAuthorized && expectedAuthorization) {
          return this.router.createUrlTree(['/login']);
        }

        return this.router.createUrlTree(['/main']);
      })
    );
  }
}

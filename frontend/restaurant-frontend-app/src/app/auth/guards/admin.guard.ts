import { CanActivate, Router } from '@angular/router';
import { AuthenticationService } from '../services/authentication.service';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AdminGuard implements CanActivate {
  private authService = inject(AuthenticationService);
  private router = inject(Router);

  // canActivate(): boolean {
  //   const user = this.authService.getUser();
  //   console.log('user.role', user?.role);

  //   if (user && user.role === 'Admin') {
  //     return true;
  //   }

  //   this.router.navigate(['/reports']);
  //   return false;
  // }
  canActivate(): boolean {
    const user = this.authService.getUser();

    if (!user || !user.role) {
      console.warn('User not authenticated or missing role');
      return false;
    }

    if (user && user.role === 'Admin') {
      return true;
    }
    this.router.navigate(['/reports']);
    return user.role === 'Admin';
  }
}

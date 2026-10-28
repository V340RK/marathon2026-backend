import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ToastComponent, ToastData } from './toast.component';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private snackBar = inject(MatSnackBar);

  error(message: string, title?: string): void {
    this.open({ message, title , type: "error"},5000, ['toast-error']);
  }

  success(message: string, title?: string): void {
    this.open({ message, title, type: "success" },3000,['toast-success']);
  }

  info(message: string, title?: string): void {
    this.open({ message,title,type: "info" }, 3000, ['toast-info']);
  }

  private open(data: ToastData,  duration: number, panelClass: string[]): void {
    this.snackBar.openFromComponent(ToastComponent, {
      data,
      duration,
      panelClass,
      verticalPosition: 'top',
      horizontalPosition: 'right',
    });
  }
}

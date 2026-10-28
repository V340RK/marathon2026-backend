import { HttpErrorResponse } from '@angular/common/http';

export function getServerErrorMessage(error: HttpErrorResponse): string {
  if (typeof error.error === 'string') {
    return error.error;
  }

  return error.error?.error ?? error.error?.message ?? '';
}

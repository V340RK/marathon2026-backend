import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormControl,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { FormValidatorsService } from '@app/auth/services/form-validators-service.service';
import { ConfirmPasswordComponent } from '@app/shared/custom-components/confirm-password/confirm-password.component';
import { PasswordComponent } from '@app/shared/custom-components/password/password.component';
import { PasswordWithRulesComponent } from '@app/shared/custom-components/password-with-rules/password-with-rules.component';
import { ToastService } from '@app/services/toast/toast.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    PasswordComponent,
    PasswordWithRulesComponent,
    ConfirmPasswordComponent,
  ],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly validatorsService = inject(FormValidatorsService);
  private readonly authenticationService = inject(AuthenticationService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly passwordForm = this.formBuilder.nonNullable.group(
    {
      currentPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(16),
        ],
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(16),
        ],
      ],
      confirmPassword: ['', Validators.required],
    },
    {
      validators: (control) =>
        this.validatorsService.passwordMatchValidator(control),
    },
  );

  get oldPassword(): FormControl<string> {
    return this.passwordForm.controls.currentPassword;
  }

  get password(): FormControl<string> {
    return this.passwordForm.controls.password;
  }

  get confirmPassword(): FormControl<string> {
    return this.passwordForm.controls.confirmPassword;
  }

  onChangePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, password } =
      this.passwordForm.getRawValue();

    if (password === currentPassword) {
      this.showError(
        'New password cannot be the same as the current password',
      );
      return;
    }

    this.authenticationService
      .changePassword(currentPassword, password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (!response) {
            this.showError('Failed to change password');
            return;
          }

          this.toastService.success(
            'Password changed successfully',
            'Success',
          );

          this.passwordForm.reset();
          this.authenticationService.signOut();
          void this.router.navigate(['/login']);
        },
        error: () => {
          this.showError('Failed to change password');
        },
      });
  }

  private showError(message: string): void {
    this.toastService.error(message, 'Error');
  }
}

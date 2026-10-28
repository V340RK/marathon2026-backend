import { Component, effect, inject, input, ChangeDetectionStrategy, signal, DestroyRef } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { startWith } from 'rxjs/operators';
import { PasswordComponent } from '../password/password.component';
import { NgClass } from '@angular/common';
import { FormValidatorsService } from '@app/auth/services/form-validators-service.service';

@Component({
  selector: 'app-password-with-rules',
  imports: [ReactiveFormsModule, PasswordComponent, NgClass],
  templateUrl: './password-with-rules.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './password-with-rules.component.scss',
})
export class PasswordWithRulesComponent {
  readonly control = input.required<FormControl>();
  readonly label = input('Password');
  readonly placeholder = input('Enter your Password');

  private formValidatorsService = inject(FormValidatorsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly passwordStrength = signal<'Weak' | 'Strong' | null>(null);
  readonly passwordStrengthColor = signal <'color-error' | 'color-success' | null>(null);

  readonly passwordValid = signal({
    hasNumber: false,
    hasUppercase: false,
    hasLowercase: false,
    hasSpecialCharacter: false,
    lengthValid: false,
  });

  constructor() {
    effect((onCleanup) => {
      const ctrl = this.control();

      const sub = ctrl.valueChanges  
        .pipe(startWith(ctrl.value ?? ''))
        .subscribe((value: string) => {
          const result = this.formValidatorsService.evaluatePasswordStrength(value || '');
          this.passwordValid.set(result.valid);
          this.passwordStrength.set(result.strength);
          this.passwordStrengthColor.set(result.color);
        });

        // cleanup on effect re-run (control rebind) and on destroy
        onCleanup(() => sub.unsubscribe());
    });
  }
}

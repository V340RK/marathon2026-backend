import { Component, input, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-password',
  imports: [ReactiveFormsModule],
  templateUrl: './password.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './password.component.scss',
})
export class PasswordComponent {
  // Inputs as signals
  readonly control = input.required<FormControl>();
  readonly label = input<string | undefined>(undefined);
  readonly placeholder = input<string>('Enter your Password');

  private static nextId = 0;
  readonly inputId = `password-input-${PasswordComponent.nextId++}`;

  // Local mutable UI state -> signals
  readonly passwordFocused = signal(false);
  readonly showPassword = signal(false);

  // Small handlers (to keep template clean)
  onFocus() {
    this.passwordFocused.set(true);
  }

  onBlur() {
    this.passwordFocused.set(false);
  }

  toggleShowPassword() {
    this.showPassword.update(v => !v);
  }
}

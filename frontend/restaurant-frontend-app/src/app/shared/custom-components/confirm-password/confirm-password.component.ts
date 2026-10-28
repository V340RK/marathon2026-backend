import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { FormControl } from '@angular/forms';
import { PasswordComponent } from '../password/password.component';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-confirm-password',
  imports: [PasswordComponent, NgClass],
  templateUrl: './confirm-password.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './confirm-password.component.scss',
})
export class ConfirmPasswordComponent {
  readonly control = input.required<FormControl>();
}

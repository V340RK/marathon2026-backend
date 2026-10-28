import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors } from '@angular/forms';
import { PasswordStrengthResult, PasswordValidity } from '../models/register.model';

@Injectable({ providedIn: 'root' })
export class FormValidatorsService {
  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password');
    const confirmPassword = group.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    const mismatch = password.value !== confirmPassword.value;
    if (mismatch) {
      confirmPassword.setErrors({ passwordMismatch: true });
    } else {
      const errors = confirmPassword.errors;
      if (errors) {
        delete errors['passwordMismatch'];
        confirmPassword.setErrors(Object.keys(errors).length ? errors : null);
      }
    }

    return null;
  }

  evaluatePasswordStrength(password: string): PasswordStrengthResult {
    const specialCharRegex = /[!@#$%^&*()_+{}[\]:;<>,.?~\\/-]/;
    const valid: PasswordValidity = {
      hasNumber: /[0-9]/.test(password),
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password),
      hasSpecialCharacter: specialCharRegex.test(password),
      lengthValid: password.length >= 8 && password.length <= 16,
    };

    const passed = Object.values(valid).filter(Boolean).length;
    return {
      valid,
      strength: passed >= 4 ? 'Strong' : 'Weak',
      color: passed >= 4 ? 'color-success' : 'color-error',
    };
  }
}

export interface PasswordStrengthResult {
  strength: 'Strong' | 'Weak';
  color: 'color-success' | 'color-error';
  valid: PasswordValidity;
}

export interface PasswordValidity {
  hasNumber: boolean;
  hasUppercase: boolean;
  hasLowercase: boolean;
  hasSpecialCharacter: boolean;
  lengthValid: boolean;
}

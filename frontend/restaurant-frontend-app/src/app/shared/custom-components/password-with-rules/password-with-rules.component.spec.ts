import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { By } from '@angular/platform-browser';

import { PasswordWithRulesComponent } from './password-with-rules.component';
import { FormValidatorsService } from '@app/auth/services/form-validators-service.service';

describe('PasswordWithRulesComponent', () => {
  let component: PasswordWithRulesComponent;
  let fixture: ComponentFixture<PasswordWithRulesComponent>;

  const validatorsServiceMock = jasmine.createSpyObj<FormValidatorsService>(
    'FormValidatorsService',
    ['evaluatePasswordStrength'],
  );

  beforeEach(async () => {
    validatorsServiceMock.evaluatePasswordStrength.and.returnValue({
      valid: {
        hasNumber: false,
        hasUppercase: false,
        hasLowercase: true,
        hasSpecialCharacter: false,
        lengthValid: false,
      },
      strength: 'Weak',
      color: 'color-error',
    });

    await TestBed.configureTestingModule({
      imports: [PasswordWithRulesComponent],
      providers: [{ provide: FormValidatorsService, useValue: validatorsServiceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(PasswordWithRulesComponent);
    component = fixture.componentInstance;

    //required input
    fixture.componentRef.setInput('control', new FormControl(''));
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call evaluatePasswordStrenght when control value changes', () => {
    const ctrl = new FormControl('');
    fixture.componentRef.setInput('control', ctrl);
    fixture.detectChanges();

    ctrl.setValue('abc'); //triggers valueChanges subscription
    ctrl.markAsTouched();
    fixture.detectChanges();

    expect(validatorsServiceMock.evaluatePasswordStrength).toHaveBeenCalled();
    expect(validatorsServiceMock.evaluatePasswordStrength).toHaveBeenCalledWith('abc');
  });

  it('should render strength indicator when control is dirty', () => {
    const ctrl = new FormControl('');
    fixture.componentRef.setInput('control', ctrl);
    fixture.detectChanges();

    ctrl.setValue('abc'); //make control dirty
    ctrl.markAsTouched();
    fixture.detectChanges();

    const strengthEl = fixture.debugElement.query(By.css('.password-strength'));
    expect(strengthEl).not.toBeNull();

    const dotEl = fixture.debugElement.query(By.css('.indicator-dot'));
    expect(dotEl.nativeElement.classList.contains('color-error')).toBeTrue();

    expect(strengthEl.nativeElement.textContent).toContain('Weak');
  });
});

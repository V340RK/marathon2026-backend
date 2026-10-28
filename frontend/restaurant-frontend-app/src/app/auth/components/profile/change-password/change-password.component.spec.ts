import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, of, Subject, throwError } from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { FormValidatorsService } from '@app/auth/services/form-validators-service.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ChangePasswordComponent } from './change-password.component';

describe('ChangePasswordComponent', () => {
  let component: ChangePasswordComponent;
  let fixture: ComponentFixture<ChangePasswordComponent>;

  let auth: {
    changePassword: jasmine.Spy;
    signOut: jasmine.Spy;
  };

  let toast: {
    success: jasmine.Spy;
    error: jasmine.Spy;
  };

  let router: {
    navigate: jasmine.Spy;
  };

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthenticationService', [
      'changePassword',
      'signOut',
    ]);

    toast = jasmine.createSpyObj('ToastService', [
      'success',
      'error',
    ]);

    router = jasmine.createSpyObj('Router', ['navigate']);

    auth.changePassword.and.returnValue(of(true));
    router.navigate.and.returnValue(Promise.resolve(true));

    await TestBed.configureTestingModule({
      imports: [ChangePasswordComponent],
      providers: [
        FormBuilder,
        FormValidatorsService,
        { provide: AuthenticationService, useValue: auth },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
      ],
    })
      .overrideComponent(ChangePasswordComponent, {
        set: {
          template: '',
          templateUrl: undefined,
          imports: [],
          styles: [],
          styleUrl: undefined,
          styleUrls: [],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ChangePasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function setPasswords(
    password = 'Newpass2',
    currentPassword = 'Oldpass1',
  ): void {
    component.passwordForm.setValue({
      currentPassword,
      password,
      confirmPassword: password,
    });
  }

  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('rejects mismatched passwords using the real form validator', () => {
    setPasswords();
    component.confirmPassword.setValue('Different3');

    expect(component.passwordForm.invalid).toBeTrue();

    component.onChangePassword();

    expect(auth.changePassword).not.toHaveBeenCalled();
  });

  for (const [length, error] of [
    [7, 'minlength'],
    [17, 'maxlength'],
  ] as const) {
    it(`rejects a new password of length ${length}`, () => {
      setPasswords('a'.repeat(length));

      expect(component.password.hasError(error)).toBeTrue();

      component.onChangePassword();

      expect(auth.changePassword).not.toHaveBeenCalled();
    });

    it(`rejects a current password of length ${length}`, () => {
      setPasswords('Newpass2', 'a'.repeat(length));

      expect(component.oldPassword.hasError(error)).toBeTrue();

      component.onChangePassword();

      expect(auth.changePassword).not.toHaveBeenCalled();
    });
  }

  for (const length of [8, 16]) {
    it(`accepts the password length boundary ${length}`, () => {
      setPasswords('a'.repeat(length));

      expect(component.passwordForm.valid).toBeTrue();
    });
  }

  it('marks required fields touched without submitting an empty form', () => {
    component.onChangePassword();

    expect(component.oldPassword.hasError('required')).toBeTrue();
    expect(component.password.hasError('required')).toBeTrue();
    expect(component.confirmPassword.hasError('required')).toBeTrue();

    expect(component.oldPassword.touched).toBeTrue();
    expect(component.password.touched).toBeTrue();
    expect(component.confirmPassword.touched).toBeTrue();

    expect(auth.changePassword).not.toHaveBeenCalled();
  });

  it('blocks reusing the current password', () => {
    setPasswords('Oldpass1', 'Oldpass1');

    component.onChangePassword();

    expect(auth.changePassword).not.toHaveBeenCalled();

    expect(toast.error).toHaveBeenCalledWith(
      'New password cannot be the same as the current password',
      'Error',
    );
  });

  it('submits the correct values, resets the form, signs out and navigates on success', () => {
    setPasswords();

    component.onChangePassword();

    expect(auth.changePassword).toHaveBeenCalledOnceWith(
      'Oldpass1',
      'Newpass2',
    );

    expect(toast.success).toHaveBeenCalledWith(
      'Password changed successfully',
      'Success',
    );

    expect(component.passwordForm.getRawValue()).toEqual({
      currentPassword: '',
      password: '',
      confirmPassword: '',
    });

    expect(auth.signOut).toHaveBeenCalledTimes(1);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('shows an error and preserves the form when the request fails', () => {
    auth.changePassword.and.returnValue(
      throwError(() => new Error('Request failed')),
    );

    setPasswords();
    component.onChangePassword();

    expect(toast.error).toHaveBeenCalledWith(
      'Failed to change password',
      'Error',
    );

    expect(component.password.value).toBe('Newpass2');

    expect(toast.success).not.toHaveBeenCalled();
    expect(auth.signOut).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('treats a falsy response as a failure', () => {
    auth.changePassword.and.returnValue(of(null));

    setPasswords();
    component.onChangePassword();

    expect(toast.error).toHaveBeenCalledWith(
      'Failed to change password',
      'Error',
    );

    expect(auth.signOut).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('unsubscribes on destruction and ignores a later response', () => {
    const pending = new Subject<boolean>();
    const teardown = jasmine.createSpy('teardown');

    auth.changePassword.and.returnValue(
      pending.pipe(finalize(teardown)),
    );

    setPasswords();
    component.onChangePassword();

    fixture.destroy();

    expect(teardown).toHaveBeenCalledTimes(1);

    pending.next(true);

    expect(toast.success).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
    expect(auth.signOut).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});

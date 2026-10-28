import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { concatMap, EMPTY, finalize, of, throwError } from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { UserModel } from '@app/auth/models/user.model';
import { UsersService } from '@app/services/users/users.service';
import { ToastService } from '@app/services/toast/toast.service';

type ProfileControlName = 'firstName' | 'lastName';

@Component({
  selector: 'app-general-user-info',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: 'general-user-info.component.html',
  styleUrl: 'general-user-info.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeneralInformationComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authenticationService = inject(AuthenticationService);
  private readonly usersService = inject(UsersService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly user = signal<UserModel | null>(null);
  readonly profileImage = signal<string | null>(null);
  readonly isSaving = signal(false);
  readonly isReadingPhoto = signal(false);

  private photoChanged = false;
  private imageRevision = 0;
  private userRevision = 0;
  private activeReader: FileReader | null = null;

  readonly userForm = this.formBuilder.nonNullable.group({
    firstName: [
      '',
      [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[A-Za-z'-]+$/),
      ],
    ],
    lastName: [
      '',
      [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[A-Za-z'-]+$/),
      ],
    ],
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.imageRevision++;
      this.stopReadingPhoto();
    });

    this.authenticationService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((user) => {
        const userChanged = this.user()?.id !== user?.id;
        this.user.set(user);

        if (userChanged) {
          this.userRevision++;
          this.imageRevision++;
          this.stopReadingPhoto();
          this.photoChanged = false;
          this.profileImage.set(null);
        }

        if (!user) {
          this.userForm.reset();
          return;
        }

        this.userForm.patchValue({
          firstName: user.firstName,
          lastName: user.lastName,
        });

        if (userChanged) {
          this.loadUserProfileImage(user.id);
        }
      });
  }

  isInvalid(controlName: ProfileControlName): boolean {
    const control = this.userForm.controls[controlName];
    return control.invalid && control.touched;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file || !this.user()) {
      return;
    }

    const revision = ++this.imageRevision;
    this.stopReadingPhoto();

    const reader = new FileReader();
    this.activeReader = reader;
    this.isReadingPhoto.set(true);

    reader.onload = () => {
      if (this.activeReader !== reader || revision !== this.imageRevision) {
        return;
      }

      this.activeReader = null;
      this.isReadingPhoto.set(false);

      if (typeof reader.result !== 'string') {
        this.handlePhotoReadError();
        return;
      }

      this.photoChanged = true;
      this.profileImage.set(reader.result);
    };

    reader.onerror = () => {
      if (this.activeReader !== reader || revision !== this.imageRevision) {
        return;
      }

      this.activeReader = null;
      this.isReadingPhoto.set(false);
      this.handlePhotoReadError();
    };

    reader.readAsDataURL(file);
  }

  onSave(): void {
    if (this.isSaving() || this.isReadingPhoto()) {
      return;
    }

    const user = this.user();
    if (!user) {
      return;
    }

    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    const { firstName, lastName } = this.userForm.getRawValue();
    const imageToUpload = this.photoChanged ? this.profileImage() : null;
    const imageRevision = this.imageRevision;
    const userRevision = this.userRevision;
    let uploadingPhoto = false;

    this.isSaving.set(true);

    this.authenticationService.editProfile(firstName, lastName)
      .pipe(
        concatMap((response) => {
          if (userRevision !== this.userRevision) {
            return EMPTY;
          }

          if (!response) {
            return throwError(() => new Error('Failed to update profile'));
          }

          if (!imageToUpload) {
            return of(null);
          }

          uploadingPhoto = true;
          return this.authenticationService.uploadMyProfileImage(imageToUpload);
        }),
        finalize(() => this.isSaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          if (userRevision !== this.userRevision) {
            return;
          }

          if (imageToUpload && imageRevision === this.imageRevision) {
            this.photoChanged = false;
          }

          this.showSuccess();
        },
        error: (error: unknown) => {
          if (userRevision !== this.userRevision) {
            return;
          }

          if (uploadingPhoto) {
            console.error('Failed to upload profile image', error);
            this.showError('Failed to upload profile image');
          } else {
            this.showError();
          }
        },
      });
  }

  private loadUserProfileImage(userId: string): void {
    const revision = this.imageRevision;

    this.usersService.getUserProfileImage(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (revision !== this.imageRevision || this.photoChanged) {
            return;
          }

          this.profileImage.set(response.presignedUrl);
        },
        error: (error: unknown) => {
          if (revision !== this.imageRevision || this.photoChanged) {
            return;
          }

          console.error('Failed to load profile image', error);
          this.profileImage.set(null);
        },
      });
  }

  private stopReadingPhoto(): void {
    const reader = this.activeReader;
    this.activeReader = null;
    this.isReadingPhoto.set(false);

    if (reader?.readyState === FileReader.LOADING) {
      reader.abort();
    }
  }

  private handlePhotoReadError(): void {
    this.showError('Failed to read profile image');

    const user = this.user();
    if (user && !this.photoChanged && !this.profileImage()) {
      this.loadUserProfileImage(user.id);
    }
  }

  private showSuccess(): void {
    this.toastService.success('Profile updated successfully', 'Success');
  }

  private showError(message = 'Failed to update profile'): void {
    this.toastService.error(message, 'Error');
  }
}

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import {
  BehaviorSubject,
  finalize,
  of,
  Subject,
  throwError,
} from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { UserModel } from '@app/auth/models/user.model';
import { UsersService } from '@app/services/users/users.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GeneralInformationComponent } from './general-user-info.component';

describe('GeneralInformationComponent', () => {
  const user = {
    id: 'user-1',
    firstName: 'Alice',
    lastName: 'Smith',
  } as UserModel;

  const serverImage = 'https://test.api/avatar.png';
  const selectedImage = 'data:image/png;base64,cGhvdG8=';

  let component: GeneralInformationComponent;
  let fixture: ComponentFixture<GeneralInformationComponent>;

  let auth: {
    user$: BehaviorSubject<UserModel | null>;
    editProfile: jasmine.Spy;
    uploadMyProfileImage: jasmine.Spy;
  };

  let users: {
    getUserProfileImage: jasmine.Spy;
  };

  let toast: {
    success: jasmine.Spy;
    error: jasmine.Spy;
  };

  let readSpy: jasmine.Spy;
  let abortSpy: jasmine.Spy;

  let readers: FileReader[];
  let images: HTMLImageElement[];

  let readerResults: WeakMap<FileReader, string>;
  let readerStates: WeakMap<FileReader, FileReader['readyState']>;

  beforeEach(async () => {
    auth = {
      user$: new BehaviorSubject<UserModel | null>(user),

      editProfile: jasmine
        .createSpy('editProfile')
        .and.returnValue(of(true)),

      uploadMyProfileImage: jasmine
        .createSpy('uploadMyProfileImage')
        .and.returnValue(of(null)),
    };

    users = {
      getUserProfileImage: jasmine
        .createSpy('getUserProfileImage')
        .and.returnValue(of({ presignedUrl: serverImage })),
    };

    toast = jasmine.createSpyObj('ToastService', [
      'success',
      'error',
    ]);

    readers = [];
    images = [];
    readerResults = new WeakMap();
    readerStates = new WeakMap();

    // Control callbacks without reading or decoding real files.
    spyOnProperty(FileReader.prototype, 'result', 'get')
      .and.callFake(function (this: FileReader) {
      return readerResults.get(this) ?? null;
    });

    spyOnProperty(FileReader.prototype, 'readyState', 'get')
      .and.callFake(function (this: FileReader) {
      return readerStates.get(this) ?? FileReader.EMPTY;
    });

    readSpy = spyOn(
      FileReader.prototype,
      'readAsDataURL',
    ).and.callFake(function (this: FileReader) {
      readers.push(this);
      readerStates.set(this, FileReader.LOADING);
    });

    abortSpy = spyOn(
      FileReader.prototype,
      'abort',
    ).and.callFake(function (this: FileReader) {
      readerStates.set(this, FileReader.DONE);
    });

    spyOnProperty(
      HTMLImageElement.prototype,
      'src',
      'set',
    ).and.callFake(function (
      this: HTMLImageElement,
      value: string,
    ) {
      if (value) {
        images.push(this);
      }
    });

    spyOn(console, 'error');

    await TestBed.configureTestingModule({
      imports: [GeneralInformationComponent],
      providers: [
        FormBuilder,
        { provide: AuthenticationService, useValue: auth },
        { provide: UsersService, useValue: users },
        { provide: ToastService, useValue: toast },
      ],
    })
      .overrideComponent(GeneralInformationComponent, {
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

    createComponent();
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(GeneralInformationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function selectFile(type = 'image/png', size = 5): void {
    const file = new File(['photo'], 'avatar', { type });

    Object.defineProperty(file, 'size', { value: size });

    const input = document.createElement('input');
    input.type = 'file';

    Object.defineProperty(input, 'files', {
      value: [file],
    });

    component.onFileSelected({
      target: input,
    } as unknown as Event);
  }

  function completeReader(
    reader: FileReader,
    data = selectedImage,
  ): void {
    readerResults.set(reader, data);
    readerStates.set(reader, FileReader.DONE);

    reader.onload?.call(
      reader,
      new ProgressEvent('load') as ProgressEvent<FileReader>,
    );
  }

  function completeImage(width = 200, height = 200): void {
    const image = images[images.length - 1];

    spyOnProperty(image, 'naturalWidth', 'get')
      .and.returnValue(width);

    spyOnProperty(image, 'naturalHeight', 'get')
      .and.returnValue(height);

    image.onload?.call(image, new Event('load'));
  }

  function selectValidPhoto(data = selectedImage): void {
    selectFile();
    completeReader(readers[readers.length - 1], data);
    completeImage();
  }

  it('creates the component and loads the current user photo', () => {
    expect(component).toBeTruthy();

    expect(users.getUserProfileImage)
      .toHaveBeenCalledOnceWith(user.id);

    expect(component.profileImage()).toBe(serverImage);

    expect(component.userForm.getRawValue()).toEqual({
      firstName: 'Alice',
      lastName: 'Smith',
    });
  });

  for (const name of ['Alice1', 'Alice!', 'Alice Smith']) {
    it(`rejects the first name "${name}"`, () => {
      component.userForm.controls.firstName.setValue(name);

      expect(
        component.userForm.controls.firstName.hasError('pattern'),
      ).toBeTrue();

      component.onSave();

      expect(auth.editProfile).not.toHaveBeenCalled();
    });
  }

  it('accepts apostrophes and hyphens in names', () => {
    component.userForm.setValue({
      firstName: 'Mary-Jane',
      lastName: "O'Neil",
    });

    expect(component.userForm.valid).toBeTrue();
  });

  for (const controlName of ['firstName', 'lastName'] as const) {
    it(`accepts 50 characters and rejects 51 in ${controlName}`, () => {
      const control = component.userForm.controls[controlName];

      control.setValue('a'.repeat(50));
      expect(control.valid).toBeTrue();

      control.setValue('a'.repeat(51));
      expect(control.hasError('maxlength')).toBeTrue();
    });
  }

  it('marks empty names touched and blocks saving', () => {
    component.userForm.reset();
    component.onSave();

    expect(
      component.userForm.controls.firstName.hasError('required'),
    ).toBeTrue();

    expect(
      component.userForm.controls.lastName.hasError('required'),
    ).toBeTrue();

    expect(component.userForm.controls.firstName.touched).toBeTrue();
    expect(component.userForm.controls.lastName.touched).toBeTrue();

    expect(auth.editProfile).not.toHaveBeenCalled();
  });

  it('does not reload the image for a same-user update', () => {
    auth.user$.next({
      ...user,
      firstName: 'Alicia',
    });

    expect(users.getUserProfileImage).toHaveBeenCalledTimes(1);

    expect(component.userForm.controls.firstName.value)
      .toBe('Alicia');
  });

  it('saves changed names with the correct arguments without uploading an unchanged photo', () => {
    component.userForm.setValue({
      firstName: 'Alicia',
      lastName: 'Jones',
    });

    component.onSave();

    expect(auth.editProfile)
      .toHaveBeenCalledOnceWith('Alicia', 'Jones');

    expect(auth.uploadMyProfileImage).not.toHaveBeenCalled();

    expect(toast.success).toHaveBeenCalledWith(
      'Profile updated successfully',
      'Success',
    );

    expect(component.isSaving()).toBeFalse();
  });

  it('shows an error when saving names fails', () => {
    auth.editProfile.and.returnValue(
      throwError(() => new Error('Save failed')),
    );

    component.userForm.controls.firstName.setValue('Alicia');
    component.onSave();

    expect(toast.error).toHaveBeenCalledWith(
      'Failed to update profile',
      'Error',
    );

    expect(toast.success).not.toHaveBeenCalled();
    expect(auth.uploadMyProfileImage).not.toHaveBeenCalled();
    expect(component.isSaving()).toBeFalse();
  });

  it('treats a falsy profile response as a failure', () => {
    auth.editProfile.and.returnValue(of(null));

    component.userForm.controls.firstName.setValue('Alicia');
    component.onSave();

    expect(toast.error).toHaveBeenCalledWith(
      'Failed to update profile',
      'Error',
    );

    expect(toast.success).not.toHaveBeenCalled();
  });

  it('uploads a selected photo without resaving unchanged names', () => {
    selectValidPhoto();
    component.onSave();

    expect(auth.editProfile).not.toHaveBeenCalled();

    expect(auth.uploadMyProfileImage)
      .toHaveBeenCalledOnceWith(selectedImage);

    component.onSave();

    expect(auth.uploadMyProfileImage).toHaveBeenCalledTimes(1);
  });

  it('reports a partial save and retries only the failed photo', () => {
    auth.uploadMyProfileImage.and.returnValue(
      throwError(() => new Error('Upload failed')),
    );

    component.userForm.controls.firstName.setValue('Alicia');

    selectValidPhoto();
    component.onSave();

    expect(auth.editProfile)
      .toHaveBeenCalledOnceWith('Alicia', 'Smith');

    expect(toast.error).toHaveBeenCalledWith(
      'Your name changes were saved, but the photo upload failed. Click Save Changes to retry the photo.',
      'Error',
    );

    expect(toast.success).not.toHaveBeenCalled();
    expect(component.profileImage()).toBe(selectedImage);
    expect(component.isSaving()).toBeFalse();

    auth.uploadMyProfileImage.and.returnValue(of(null));

    component.onSave();

    expect(auth.editProfile).toHaveBeenCalledTimes(1);
    expect(auth.uploadMyProfileImage).toHaveBeenCalledTimes(2);

    expect(toast.success).toHaveBeenCalledWith(
      'Profile updated successfully',
      'Success',
    );
  });

  it('handles FileReader errors and preserves the displayed photo', () => {
    selectFile();

    const reader = readers[0];
    readerStates.set(reader, FileReader.DONE);

    reader.onerror?.call(
      reader,
      new ProgressEvent('error') as ProgressEvent<FileReader>,
    );

    expect(toast.error).toHaveBeenCalledWith(
      'Failed to read profile image',
      'Error',
    );

    expect(component.profileImage()).toBe(serverImage);
    expect(component.isReadingPhoto()).toBeFalse();
    expect(auth.uploadMyProfileImage).not.toHaveBeenCalled();
  });

  for (const type of ['text/plain', 'image/svg+xml']) {
    it(`rejects unsupported file type ${type} before reading`, () => {
      selectFile(type);

      expect(readSpy).not.toHaveBeenCalled();

      expect(toast.error).toHaveBeenCalledWith(
        'Please select a JPEG, PNG, or WebP image',
        'Error',
      );
    });
  }

  it('rejects files larger than 5 MB before reading', () => {
    selectFile('image/png', 5 * 1024 * 1024 + 1);

    expect(readSpy).not.toHaveBeenCalled();

    expect(toast.error).toHaveBeenCalledWith(
      'Image must be 5 MB or smaller',
      'Error',
    );
  });

  it('rejects empty files before reading', () => {
    selectFile('image/png', 0);

    expect(readSpy).not.toHaveBeenCalled();

    expect(toast.error).toHaveBeenCalledWith(
      'Please select a non-empty image file',
      'Error',
    );
  });

  it('accepts the exact size and dimension limits', () => {
    selectFile('image/png', 5 * 1024 * 1024);
    completeReader(readers[0]);
    completeImage(4096, 4096);

    expect(component.profileImage()).toBe(selectedImage);
    expect(component.isReadingPhoto()).toBeFalse();
    expect(toast.error).not.toHaveBeenCalled();
  });

  for (const [width, height] of [
    [4097, 200],
    [200, 4097],
  ]) {
    it(`rejects dimensions ${width} × ${height}`, () => {
      selectFile();
      completeReader(readers[0]);
      completeImage(width, height);

      expect(component.profileImage()).toBe(serverImage);

      expect(toast.error).toHaveBeenCalledWith(
        'Image dimensions must not exceed 4096 × 4096 pixels',
        'Error',
      );
    });
  }

  it('rejects images that cannot be decoded', () => {
    selectFile();
    completeReader(readers[0]);

    const image = images[0];
    image.onerror?.call(image, new Event('error'));

    expect(component.profileImage()).toBe(serverImage);
    expect(component.isReadingPhoto()).toBeFalse();

    expect(toast.error).toHaveBeenCalledWith(
      'Please select a valid image file',
      'Error',
    );
  });

  it('blocks saving while a file is being read or validated', () => {
    component.userForm.controls.firstName.setValue('Alicia');

    selectFile();
    component.onSave();

    completeReader(readers[0]);
    component.onSave();

    expect(auth.editProfile).not.toHaveBeenCalled();
    expect(auth.uploadMyProfileImage).not.toHaveBeenCalled();

    completeImage();
    component.onSave();

    expect(auth.editProfile).toHaveBeenCalledTimes(1);
    expect(auth.uploadMyProfileImage).toHaveBeenCalledTimes(1);
  });

  it('prevents duplicate submissions while saving', () => {
    const pending = new Subject<boolean>();

    auth.editProfile.and.returnValue(pending);

    component.userForm.controls.firstName.setValue('Alicia');

    component.onSave();
    component.onSave();

    expect(auth.editProfile).toHaveBeenCalledTimes(1);

    pending.next(true);
    pending.complete();

    expect(component.isSaving()).toBeFalse();
  });

  it('ignores an old image fetch after a new photo was saved', () => {
    fixture.destroy();

    const pending = new Subject<{ presignedUrl: string }>();

    users.getUserProfileImage.and.returnValue(pending);

    createComponent();

    selectValidPhoto();
    component.onSave();

    pending.next({
      presignedUrl: 'https://test.api/stale.png',
    });

    expect(component.profileImage()).toBe(selectedImage);
  });

  it('does not mark a newer selection saved when an earlier upload finishes', () => {
    const pending = new Subject<null>();

    auth.uploadMyProfileImage.and.returnValue(pending);

    selectValidPhoto();
    component.onSave();

    const newerImage = 'data:image/png;base64,bmV3';

    selectValidPhoto(newerImage);

    pending.next(null);
    pending.complete();

    auth.uploadMyProfileImage.and.returnValue(of(null));

    component.onSave();

    expect(auth.uploadMyProfileImage).toHaveBeenCalledTimes(2);
    expect(auth.uploadMyProfileImage)
      .toHaveBeenCalledWith(newerImage);

    expect(component.profileImage()).toBe(newerImage);
  });

  it('handles a profile image fetch failure', () => {
    users.getUserProfileImage.and.returnValue(
      throwError(() => new Error('Image fetch failed')),
    );

    auth.user$.next({
      ...user,
      id: 'user-2',
    });

    expect(component.profileImage()).toBeNull();
    expect(console.error).toHaveBeenCalled();
  });

  it('tears down a pending name save when destroyed', () => {
    const pending = new Subject<boolean>();
    const teardown = jasmine.createSpy('teardown');

    auth.editProfile.and.returnValue(
      pending.pipe(finalize(teardown)),
    );

    component.userForm.controls.firstName.setValue('Alicia');

    selectValidPhoto();
    component.onSave();

    fixture.destroy();

    expect(teardown).toHaveBeenCalledTimes(1);

    pending.next(true);

    expect(auth.uploadMyProfileImage).not.toHaveBeenCalled();
    expect(toast.success).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('tears down a pending photo upload when destroyed', () => {
    const pending = new Subject<null>();
    const teardown = jasmine.createSpy('teardown');

    auth.uploadMyProfileImage.and.returnValue(
      pending.pipe(finalize(teardown)),
    );

    selectValidPhoto();
    component.onSave();

    fixture.destroy();

    expect(teardown).toHaveBeenCalledTimes(1);

    pending.next(null);

    expect(toast.success).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('tears down a pending image fetch and stops listening for user updates', () => {
    fixture.destroy();

    const pending = new Subject<{ presignedUrl: string }>();
    const teardown = jasmine.createSpy('teardown');

    users.getUserProfileImage.and.returnValue(
      pending.pipe(finalize(teardown)),
    );

    createComponent();

    const callCount = users.getUserProfileImage.calls.count();

    fixture.destroy();

    expect(teardown).toHaveBeenCalledTimes(1);

    pending.next({ presignedUrl: serverImage });

    auth.user$.next({
      ...user,
      id: 'user-2',
    });

    expect(component.profileImage()).toBeNull();
    expect(users.getUserProfileImage)
      .toHaveBeenCalledTimes(callCount);
  });

  it('aborts an unfinished FileReader when destroyed', () => {
    selectFile();

    fixture.destroy();

    expect(abortSpy).toHaveBeenCalledTimes(1);

    completeReader(readers[0]);

    expect(images.length).toBe(0);
    expect(component.profileImage()).toBe(serverImage);
  });
});

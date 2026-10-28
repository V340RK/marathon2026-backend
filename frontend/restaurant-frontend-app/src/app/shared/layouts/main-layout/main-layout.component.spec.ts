import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { NotificationsService } from '@app/services/notifications/notifications.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { MainLayoutComponent } from './main-layout.component';

describe('MainLayoutComponent', () => {
  let component: MainLayoutComponent;
  let fixture: ComponentFixture<MainLayoutComponent>;

  beforeEach(async () => {
    const authMock = jasmine.createSpyObj<AuthenticationService>(
      'AuthenticationService',
      ['signOut'],
      { user$: of(null), isAuthorised$: of(false) },
    );
    const notificationsMock = jasmine.createSpyObj<NotificationsService>(
      'NotificationsService',
      ['loadNotifications', 'markAsRead'],
    );

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        provideRouter([]),
        { provide: AuthenticationService, useValue: authMock },
        { provide: NotificationsService, useValue: notificationsMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

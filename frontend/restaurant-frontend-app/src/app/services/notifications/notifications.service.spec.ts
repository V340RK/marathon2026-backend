import { TestBed } from '@angular/core/testing';
import { NotificationsService } from './notifications.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('NotificationsService', () => {
  let service: NotificationsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
    });
    service = TestBed.inject(NotificationsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

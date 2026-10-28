import { TestBed } from '@angular/core/testing';
import { WaitersService } from './waiters.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('WaitersService', () => {
  let service: WaitersService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
    });
    service = TestBed.inject(WaitersService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

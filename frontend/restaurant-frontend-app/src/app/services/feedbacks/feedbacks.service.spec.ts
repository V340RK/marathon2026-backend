import { TestBed } from '@angular/core/testing';
import { FeedbacksService } from './feedbacks.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('FeedbacksService', () => {
  let service: FeedbacksService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
    });
    service = TestBed.inject(FeedbacksService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

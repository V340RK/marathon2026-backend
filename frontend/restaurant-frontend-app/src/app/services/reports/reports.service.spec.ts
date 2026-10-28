import { TestBed } from '@angular/core/testing';
import { ReportsService } from './reports.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('ReportsService', () => {
  let service: ReportsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
    });
    service = TestBed.inject(ReportsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import { LocationsService } from './locations.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('LocationsService', () => {
  let service: LocationsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
    });
    service = TestBed.inject(LocationsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

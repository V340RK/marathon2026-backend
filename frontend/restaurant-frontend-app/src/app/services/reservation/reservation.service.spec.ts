import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReservationService } from './reservation.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';

const TIME_RANGE = { start: '2026-10-17T12:00:00.000Z', end: '2026-10-17T13:30:00.000Z' };

describe('ReservationService', () => {
  let service: ReservationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
      ],
    });
    service = TestBed.inject(ReservationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('makeReservation$', () => {
    it('posts the table, time range and guest count', () => {
      service.makeReservation$('table-1', TIME_RANGE, 2).subscribe();

      const req = httpMock.expectOne('https://test.api/reservations');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        tableId: 'table-1',
        startTime: TIME_RANGE.start,
        endTime: TIME_RANGE.end,
        guestCount: 2,
      });
      req.flush({});
    });

    it('keeps the global error toast by default', () => {
      service.makeReservation$('table-1', TIME_RANGE, 2).subscribe();

      const req = httpMock.expectOne('https://test.api/reservations');
      expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeFalse();
      req.flush({});
    });

    it('marks the request to skip the global error toast when asked', () => {
      service.makeReservation$('table-1', TIME_RANGE, 2, null, true).subscribe();

      const req = httpMock.expectOne('https://test.api/reservations');
      expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeTrue();
      req.flush({});
    });
  });
});

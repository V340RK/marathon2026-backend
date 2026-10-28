import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TableService } from './table.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { Table } from '@app/models/tables/table.model';
import { TableFilter } from '@app/models/tables/table-filter.model';
import { TimeRange } from '@app/models/tables/time-range.model';

describe('TableService', () => {
  let service: TableService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
      ],
    });
    service = TestBed.inject(TableService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('sends locationId when a location is set', () => {
    service
      .getAvailableTables$({ location: 'loc-1', date: 'Oct 17, 2026', time: '10:00', guests: 2 })
      .subscribe();

    const req = httpMock.expectOne((request) => request.url === 'https://test.api/available-tables');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('locationId')).toBe('loc-1');
    expect(req.request.params.get('capacity')).toBe('2');
    req.flush([]);
  });

  it('omits locationId when no location is set', () => {
    service
      .getAvailableTables$({ location: null, date: 'Oct 17, 2026', time: '10:00', guests: 2 })
      .subscribe();

    const req = httpMock.expectOne((request) => request.url === 'https://test.api/available-tables');
    expect(req.request.params.has('locationId')).toBeFalse();
    expect(req.request.params.get('capacity')).toBe('2');
    req.flush([]);
  });

  describe('getAvailableTablesWithTimeSlots$', () => {
    const filter: TableFilter = { location: null, date: 'Oct 17, 2026', time: '10:00', guests: 2 };
    const tables: Table[] = [
      { id: 'table-1', locationId: 'loc-1', capacity: 4, tableNumber: 1, timeRanges: [] },
      { id: 'table-2', locationId: 'loc-2', capacity: 4, tableNumber: 2, timeRanges: [] },
    ];
    const slot: TimeRange = { start: '2026-10-17T10:00:00Z', end: '2026-10-17T11:30:00Z' };
    const slotsUrl = (tableId: string): string =>
      `https://test.api/tables/${tableId}/available-time-slots`;

    const flushTables = (payload: Table[]): void => {
      httpMock
        .expectOne((request) => request.url === 'https://test.api/available-tables')
        .flush(payload);
    };

    it('emits an empty list when no tables are available', () => {
      let result: Table[] | undefined;
      service.getAvailableTablesWithTimeSlots$(filter).subscribe((value) => (result = value));

      flushTables([]);

      expect(result).toEqual([]);
    });

    it('attaches time slots to every table and keeps the API order', () => {
      let result: Table[] | undefined;
      service.getAvailableTablesWithTimeSlots$(filter).subscribe((value) => (result = value));
      flushTables(tables);

      const secondRequest = httpMock.expectOne((request) => request.url === slotsUrl('table-2'));
      expect(secondRequest.request.method).toBe('GET');
      secondRequest.flush([slot]);
      httpMock.expectOne((request) => request.url === slotsUrl('table-1')).flush([]);

      expect(result?.map((table) => table.id)).toEqual(['table-1', 'table-2']);
      expect(result?.[1].timeRanges).toEqual([slot]);
    });

    it('keeps a table with no slots when its time-slot request fails', () => {
      let result: Table[] | undefined;
      service.getAvailableTablesWithTimeSlots$(filter).subscribe((value) => (result = value));
      flushTables(tables);

      httpMock
        .expectOne((request) => request.url === slotsUrl('table-1'))
        .flush('error', { status: 500, statusText: 'Server Error' });
      httpMock.expectOne((request) => request.url === slotsUrl('table-2')).flush([slot]);

      expect(result?.length).toBe(2);
      expect(result?.[0].timeRanges).toEqual([]);
      expect(result?.[1].timeRanges).toEqual([slot]);
    });

    it('sends at most 6 time-slot requests at once', () => {
      const manyTables = Array.from({ length: 10 }, (_, position) => ({
        ...tables[0],
        id: `table-${position}`,
      }));
      service.getAvailableTablesWithTimeSlots$(filter).subscribe();
      flushTables(manyTables);

      const firstBatch = httpMock.match((request) => request.url.endsWith('/available-time-slots'));
      expect(firstBatch.length).toBe(6);
      firstBatch.forEach((request) => request.flush([]));

      const secondBatch = httpMock.match((request) => request.url.endsWith('/available-time-slots'));
      expect(secondBatch.length).toBe(4);
      secondBatch.forEach((request) => request.flush([]));
    });
  });
});

import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { HttpClient } from '@angular/common/http';
import { Table } from '@app/models/tables/table.model';
import { catchError, from, map, mergeMap, Observable, of, switchMap, toArray } from 'rxjs';
import { TableFilter } from '@app/models/tables/table-filter.model';
import { revertFormatTime } from '@app/shared/helpers/date-helpers';
import { TimeRange } from '@app/models/tables/time-range.model';
import { ReservationCustomer } from '@app/models/reservations/reservation.model';

const TIME_SLOTS_CONCURRENCY = 6;

@Injectable({
  providedIn: 'root',
})
export class TableService {
  private readonly baseUrl = inject(API_BASE_URL);
  private http: HttpClient = inject(HttpClient);

  // Without a location the API returns tables of every location (160+), so time-slot requests
  // are throttled and a failure for one table leaves its slots empty instead of failing the search.
  getAvailableTablesWithTimeSlots$(filter: TableFilter): Observable<Table[]> {
    return this.getAvailableTables$(filter).pipe(
      switchMap((tables) => {
        if (tables.length === 0) {
          return of<Table[]>([]);
        }

        return from(tables).pipe(
          mergeMap(
            (table, index) =>
              this.getTableTimeSlots(table.id, filter.date!, filter.time!).pipe(
                catchError(() => of<TimeRange[]>([])),
                map((timeRanges) => ({ index, table: { ...table, timeRanges: timeRanges ?? [] } }))
              ),
            TIME_SLOTS_CONCURRENCY
          ),
          toArray(),
          map((results) =>
            results.sort((left, right) => left.index - right.index).map((result) => result.table)
          )
        );
      })
    );
  }

  getAvailableTables$(filter: TableFilter): Observable<Table[]> {
    const url = `${this.baseUrl}/available-tables`;

    const params: Record<string, string> = {
      startTime: revertFormatTime(filter.time!, filter.date!),
      capacity: filter.guests!.toString(),
    };

    if (filter.location) {
      params['locationId'] = filter.location.toString();
    }

    return this.http.get<Table[]>(url, { params });
  }

  getTableTimeSlots(tableId: string, date: string, time: string): Observable<TimeRange[]> {
    const url = `${this.baseUrl}/tables/${tableId}/available-time-slots`;
    const params = {
      startTime: revertFormatTime(time, date),
    };

    return this.http.get<TimeRange[]>(url, { params });
  }

  getAvailableTablesWithoutStartTime$(filter: TableFilter): Observable<Table[]> {
    const url = `${this.baseUrl}/available-tables`;

    const params = {
      locationId: filter.location!.toString(),
      capacity: filter.guests!.toString(),
    };

    return this.http.get<Table[]>(url, { params });
  }

  changeReservationTable$(reservationId: string, tableId: string): Observable<ReservationCustomer> {
    const url = `${this.baseUrl}/reservations/${reservationId}/table`;
    return this.http.patch<ReservationCustomer>(url, { tableId });
  }
}

import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { TableService } from '@app/services/table/table.service';
import { ToastService } from '@app/services/toast/toast.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { LocationResponse } from '@app/models/locations/location-response.model';
import { Table } from '@app/models/tables/table.model';
import { TablesComponent } from './tables.component';

const CREATED_RESERVATION: Reservation = {
  id: 'res-1',
  tableId: 'table-1',
  customerId: 'customer-1',
  startTime: new Date('2026-10-17T12:00:00Z'),
  endTime: new Date('2026-10-17T14:00:00Z'),
  guestCount: 2,
  status: ReservationStatus.Reserved,
  locationAddress: '48 Rustaveli Avenue',
  tableCapacity: 4,
  waiterId: 'waiter-1',
};

describe('ReservationsPageComponent', () => {
  let component: TablesComponent;
  let fixture: ComponentFixture<TablesComponent>;
  let reservationService: jasmine.SpyObj<ReservationService>;
  let toastService: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    reservationService = jasmine.createSpyObj<ReservationService>('ReservationService', [
      'makeReservation$',
    ]);
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['error', 'success']);

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: ReservationService, useValue: reservationService },
        { provide: ToastService, useValue: toastService },
      ],
      imports: [TablesComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TablesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not preselect a date, so the date placeholder is shown', () => {
    expect(component.selectedFilter.date).toBeNull();
  });

  it('initializes time options for the first available date', () => {
    expect(component.timeOptions.length).toBeGreaterThan(0);
  });

  it('refreshes time options and stores the date when a date is selected', () => {
    component.onDateSelected(component.dateOptions[0]);

    expect(component.selectedFilter.date).toBe(component.dateOptions[0]);
  });

  it('shows errors for date and time and does not search when both are empty', () => {
    const tableService = TestBed.inject(TableService);
    const searchSpy = spyOn(tableService, 'getAvailableTablesWithTimeSlots$').and.returnValue(
      of([])
    );

    component.onFilter();
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const messages = Array.from(element.querySelectorAll('.error-message')).map((node) =>
      node.textContent?.trim()
    );
    expect(messages).toEqual(['Choose date from the calendar', 'Choose time slot']);
    expect(searchSpy).not.toHaveBeenCalled();
  });

  it('clears the time error once a time is selected', () => {
    component.onFilter();
    component.onTimeSelected('10:00');

    expect(component.timeError).toBe('');
    expect(component.dateError).not.toBe('');
  });

  it('searches without a location when only date and time are set', () => {
    const tableService = TestBed.inject(TableService);
    const searchSpy = spyOn(tableService, 'getAvailableTablesWithTimeSlots$').and.returnValue(
      of([])
    );
    component.onDateSelected(component.dateOptions[0]);
    component.onTimeSelected(component.timeOptions[0]);

    component.onFilter();

    expect(component.dateError).toBe('');
    expect(component.timeError).toBe('');
    expect(searchSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({ location: null, guests: 1 })
    );
  });

  describe('location select', () => {
    it('stores the chosen address as the location filter', () => {
      component.onLocationSelected('48 Rustaveli Avenue');

      expect(component.selectedFilter.location).toBe('48 Rustaveli Avenue');
    });

    it('clears the location filter when All Locations is chosen', () => {
      component.selectedFilter.location = '48 Rustaveli Avenue';

      component.onLocationSelected('All Locations');

      expect(component.selectedFilter.location).toBeNull();
    });
  });

  describe('loadAvailableTables', () => {
    beforeEach(() => {
      component.lastAppliedFilter = { location: null, date: 'Oct 17, 2026', time: '10:00', guests: 2 };
    });

    it('shows a toast and clears the results when the search fails', () => {
      const tableService = TestBed.inject(TableService);
      spyOn(tableService, 'getAvailableTablesWithTimeSlots$').and.returnValue(
        throwError(() => new Error('network'))
      );
      component.tables = [
        { id: 'table-1', tableNumber: 1, locationId: 'loc-1', capacity: 4, timeRanges: [] },
      ];

      component.loadAvailableTables();

      expect(component.tables).toEqual([]);
      expect(toastService.error).toHaveBeenCalledWith(
        'Could not load available tables. Please try again.'
      );
    });
  });

  describe('loading state', () => {
    const TABLE: Table = {
      id: 'table-1',
      tableNumber: 1,
      locationId: 'loc-1',
      capacity: 4,
      timeRanges: [],
    };
    let search$: Subject<Table[]>;
    let searchSpy: jasmine.Spy;

    beforeEach(() => {
      search$ = new Subject<Table[]>();
      searchSpy = spyOn(
        TestBed.inject(TableService),
        'getAvailableTablesWithTimeSlots$'
      ).and.returnValue(search$);
      component.onDateSelected(component.dateOptions[0]);
      component.onTimeSelected(component.timeOptions[0]);
      component.onFilter();
      fixture.detectChanges();
    });

    it('shows the loader instead of the empty state while tables are loading', () => {
      const element: HTMLElement = fixture.nativeElement;

      expect(component.isLoading).toBeTrue();
      expect(element.querySelector('.loading-state')).not.toBeNull();
      expect(element.querySelector('.empty-state')).toBeNull();
    });

    it('disables the search button while tables are loading', () => {
      const button: HTMLButtonElement = fixture.nativeElement.querySelector('.booking-card button');

      expect(button.disabled).toBeTrue();
    });

    it('shows the empty state once the search returns no tables', () => {
      search$.next([]);
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(component.isLoading).toBeFalse();
      expect(element.querySelector('.loading-state')).toBeNull();
      expect(element.querySelector('.empty-state h3')?.textContent).toContain(
        'No Tables Available'
      );
    });

    it('shows the table cards once the search returns tables', () => {
      search$.next([TABLE]);
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(element.querySelector('.loading-state')).toBeNull();
      expect(element.querySelector('.empty-state')).toBeNull();
      expect(element.querySelectorAll('app-table-card').length).toBe(1);
    });

    it('stops loading and shows a toast when the search fails', () => {
      search$.error(new Error('network'));
      fixture.detectChanges();

      expect(component.isLoading).toBeFalse();
      expect(toastService.error).toHaveBeenCalledWith(
        'Could not load available tables. Please try again.'
      );
    });

    it('ignores the result of an earlier search once a newer one has started', () => {
      const newerSearch$ = new Subject<Table[]>();
      searchSpy.and.returnValue(newerSearch$);

      component.onFilter();
      search$.next([TABLE]);

      expect(component.tables).toEqual([]);
      expect(component.isLoading).toBeTrue();

      newerSearch$.next([]);

      expect(component.isLoading).toBeFalse();
    });
  });

  describe('empty state', () => {
    beforeEach(() => {
      component.lastAppliedFilter = { location: null, date: 'Oct 17, 2026', time: '10:00', guests: 2 };
      component.filterSummary = 'All Locations · Oct 17, 2026 · 10:00 · 2 guests';
      component.tables = [];
      fixture.detectChanges();
    });

    it('renders the illustration and message when no tables are found', () => {
      const element: HTMLElement = fixture.nativeElement;

      expect(element.querySelector('.empty-state h3')?.textContent).toContain('No Tables Available');
      expect(element.querySelector('.empty-state img')).not.toBeNull();
    });

    it('labels the link with the searched date', () => {
      const link: HTMLElement = fixture.nativeElement.querySelector('.show-all-link');

      expect(link.textContent?.trim()).toBe('Show all slots on Oct 17, 2026');
    });

    it('searches all locations with the same date, time and guests when the link is clicked', () => {
      spyOn(component, 'loadAvailableTables');
      component.lastAppliedFilter = {
        location: 'Rustaveli Avenue, 48',
        date: 'Oct 17, 2026',
        time: '10:00',
        guests: 2,
      };
      component.selectedFilter = { ...component.lastAppliedFilter };

      component.onShowAllSlots();

      expect(component.lastAppliedFilter).toEqual({
        location: null,
        date: 'Oct 17, 2026',
        time: '10:00',
        guests: 2,
      });
      expect(component.filterSummary).toContain('All Locations');
      expect(component.loadAvailableTables).toHaveBeenCalled();
    });
  });

  describe('filterSummary', () => {
    const applyFilter = (location: string | null, guests: number): void => {
      component.selectedFilter = { location, date: 'Oct 17, 2026', time: '11:00', guests };
      spyOn(component, 'loadAvailableTables');
      component.onFilter();
    };

    it('shows the selected location, date, time and guests', () => {
      applyFilter('48 Rustaveli Avenue', 2);

      expect(component.filterSummary).toBe('48 Rustaveli Avenue · Oct 17, 2026 · 11:00 · 2 guests');
    });

    it('shows All Locations when no location is selected', () => {
      applyFilter(null, 2);

      expect(component.filterSummary).toBe('All Locations · Oct 17, 2026 · 11:00 · 2 guests');
    });

    it('uses the singular form for one guest', () => {
      applyFilter(null, 1);

      expect(component.filterSummary).toBe('All Locations · Oct 17, 2026 · 11:00 · 1 guest');
    });
  });

  describe('getLocationName', () => {
    const table: Table = {
      id: 'table-1',
      tableNumber: 1,
      locationId: 'loc-1',
      capacity: 4,
      timeRanges: [],
    };

    it('returns the address of the matching location', () => {
      component.locations = [{ id: 'loc-1', address: '48 Rustaveli Avenue' } as LocationResponse];

      expect(component.getLocationName(table)).toBe('48 Rustaveli Avenue');
    });

    it('falls back to the filtered location when the table location is not loaded', () => {
      component.locations = [];
      component.lastAppliedFilter = {
        location: '14 Chavchavadze Avenue',
        date: 'Oct 17, 2026',
        time: '10:00',
        guests: 2,
      };

      expect(component.getLocationName(table)).toBe('14 Chavchavadze Avenue');
    });

    it('returns a placeholder when the location is unknown and no location was filtered', () => {
      component.locations = [];
      component.lastAppliedFilter = { location: null, date: 'Oct 17, 2026', time: '10:00', guests: 2 };

      expect(component.getLocationName(table)).toBe('Location unavailable');
    });
  });

  describe('selectedLocationName', () => {
    const table: Table = {
      id: 'table-1',
      tableNumber: 1,
      locationId: 'loc-1',
      capacity: 4,
      timeRanges: [],
    };

    it('returns the address of the selected table location', () => {
      component.locations = [{ id: 'loc-1', address: '48 Rustaveli Avenue' } as LocationResponse];
      component.selectedTable = table;

      expect(component.selectedLocationName).toBe('48 Rustaveli Avenue');
    });

    it('falls back to the filtered location when the table location is not loaded', () => {
      component.locations = [];
      component.selectedTable = table;
      component.lastAppliedFilter = {
        location: '14 Chavchavadze Avenue',
        date: 'Oct 17, 2026',
        time: '10:00',
        guests: 2,
      };

      expect(component.selectedLocationName).toBe('14 Chavchavadze Avenue');
    });

    it('returns a placeholder when nothing identifies the location', () => {
      component.locations = [];
      component.selectedTable = table;
      component.lastAppliedFilter = null;

      expect(component.selectedLocationName).toBe('Location unavailable');
    });
  });

  describe('review modal', () => {
    const table: Table = {
      id: 'table-1',
      tableNumber: 1,
      locationId: 'loc-1',
      capacity: 4,
      timeRanges: [],
    };
    const timeRange = { start: '2026-10-17T12:00:00Z', end: '2026-10-17T14:00:00Z' };

    beforeEach(() => {
      component.lastAppliedFilter = { location: null, date: 'Oct 17, 2026', time: '12:00', guests: 2 };
      component.selectedFilter.date = 'Oct 17, 2026';
    });

    it('opens the review modal instead of make-reservation when a time slot is chosen', () => {
      component.onTimeSlotsClick(table, timeRange);
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(element.querySelector('app-reservation-review-modal')).not.toBeNull();
      expect(element.querySelector('app-make-reservation')).toBeNull();
    });

    it('opens Edit Reservation instead of review when Edit is clicked', () => {
      component.onTimeSlotsClick(table, timeRange);

      component.onEditReview();
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(element.querySelector('app-reservation-review-modal')).toBeNull();
      expect(element.querySelector('app-edit-reservation-modal')).not.toBeNull();
    });

    it('returns to review with the edited draft after Save Changes, without creating a reservation', () => {
      const editedRange = { start: '2026-10-17T13:00:00Z', end: '2026-10-17T16:00:00Z' };
      component.onTimeSlotsClick(table, timeRange);
      component.onEditReview();

      component.onSaveDraft({ timeRange: editedRange, guests: 3 });
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(component.selectedTimeRange).toEqual(editedRange);
      expect(component.draftGuests).toBe(3);
      expect(element.querySelector('app-edit-reservation-modal')).toBeNull();
      expect(element.querySelector('app-reservation-review-modal')).not.toBeNull();
      expect(reservationService.makeReservation$).not.toHaveBeenCalled();
    });

    it('shows a success toast after Save Changes', () => {
      component.onTimeSlotsClick(table, timeRange);
      component.onEditReview();

      component.onSaveDraft({ timeRange, guests: 3 });

      expect(toastService.success).toHaveBeenCalledOnceWith(
        'Your reservation has been updated successfully.'
      );
    });

    it('does not show a success toast when Edit Reservation is closed without saving', () => {
      component.onTimeSlotsClick(table, timeRange);
      component.onEditReview();

      component.onCloseEditDraftModal();

      expect(toastService.success).not.toHaveBeenCalled();
    });

    it('returns to review with the unchanged draft when Edit Reservation is closed', () => {
      component.onTimeSlotsClick(table, timeRange);
      component.onEditReview();

      component.onCloseEditDraftModal();

      expect(component.showReviewModal).toBeTrue();
      expect(component.selectedTimeRange).toEqual(timeRange);
    });

    it('sends the edited stay length to the API', () => {
      reservationService.makeReservation$.and.returnValue(of(CREATED_RESERVATION));
      component.onTimeSlotsClick(table, timeRange);
      component.onSaveDraft({
        timeRange: { start: '2026-10-17T12:00:00Z', end: '2026-10-17T15:00:00Z' },
        guests: 3,
      });

      component.onConfirmReview();

      const [, sentRange, guests] = reservationService.makeReservation$.calls.mostRecent().args;
      expect(Date.parse(sentRange.end) - Date.parse(sentRange.start)).toBe(3 * 60 * 60 * 1000);
      expect(guests).toBe(3);
    });

    it('disables Confirm while the reservation request is pending', () => {
      reservationService.makeReservation$.and.returnValue(new Subject<Reservation>());
      component.onTimeSlotsClick(table, timeRange);

      component.onConfirmReview();
      fixture.detectChanges();

      const confirm = fixture.nativeElement.querySelector(
        'app-reservation-review-modal .confirm-btn'
      ) as HTMLButtonElement;
      expect(component.isSubmittingReservation).toBeTrue();
      expect(confirm.disabled).toBeTrue();
    });

    it('ignores a second Confirm while the first request is pending', () => {
      reservationService.makeReservation$.and.returnValue(new Subject<Reservation>());
      component.onTimeSlotsClick(table, timeRange);

      component.onConfirmReview();
      component.onConfirmReview();

      expect(reservationService.makeReservation$).toHaveBeenCalledTimes(1);
    });

    it('asks the interceptor not to show its own toast', () => {
      reservationService.makeReservation$.and.returnValue(of(CREATED_RESERVATION));
      component.onTimeSlotsClick(table, timeRange);

      component.onConfirmReview();

      expect(reservationService.makeReservation$.calls.mostRecent().args[4]).toBeTrue();
    });

    describe('when the reservation fails', () => {
      const failWith = (status: number, body: unknown): void => {
        reservationService.makeReservation$.and.returnValue(
          throwError(() => new HttpErrorResponse({ status, error: body }))
        );
        component.onTimeSlotsClick(table, timeRange);
        component.onConfirmReview();
        fixture.detectChanges();
      };

      it('shows a friendly message for an overlapping reservation', () => {
        failWith(400, {
          message: "Reservation from 08:00 to 11:00 overlaps with an existing reservation for table 'abc'.",
        });

        expect(toastService.error).toHaveBeenCalledWith(
          "This table isn't free for the whole selected time. Please choose a shorter stay or another slot."
        );
      });

      it('keeps the review modal open with the same draft and re-enables Confirm', () => {
        failWith(400, { message: 'overlaps' });

        const element: HTMLElement = fixture.nativeElement;
        expect(element.querySelector('app-reservation-review-modal')).not.toBeNull();
        expect(element.querySelector('app-reservation-invite-modal')).toBeNull();
        expect(component.selectedTimeRange).toEqual(timeRange);
        expect(component.isSubmittingReservation).toBeFalse();
      });

      it('passes through other validation messages from the server', () => {
        failWith(400, { message: 'Guest count (5) exceeds table capacity (4).' });

        expect(toastService.error).toHaveBeenCalledWith('Guest count (5) exceeds table capacity (4).');
      });

      it('asks the user to sign in on 401', () => {
        failWith(401, null);

        expect(toastService.error).toHaveBeenCalledWith('Please sign in to make a reservation.');
      });

      it('shows a generic message for a server error', () => {
        failWith(500, { message: 'NullReferenceException' });

        expect(toastService.error).toHaveBeenCalledWith(
          'Could not create the reservation. Please try again.'
        );
      });
    });

    it('creates the reservation for the selected table and guests on Confirm', () => {
      reservationService.makeReservation$.and.returnValue(of(CREATED_RESERVATION));
      component.onTimeSlotsClick(table, timeRange);

      component.onConfirmReview();
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      expect(reservationService.makeReservation$).toHaveBeenCalledWith(
        'table-1',
        jasmine.any(Object),
        2,
        null,
        true
      );
      expect(element.querySelector('app-reservation-review-modal')).toBeNull();
      expect(element.querySelector('app-reservation-invite-modal')).not.toBeNull();
    });
  });

  it('shows the invite modal instead of the old confirmation after a reservation is made', () => {
    reservationService.makeReservation$.and.returnValue(of(CREATED_RESERVATION));
    component.selectedFilter.date = 'Oct 17, 2026';

    component.onMakeReservation(
      'table-1',
      { start: '2026-10-17T12:00:00Z', end: '2026-10-17T14:00:00Z' },
      2
    );
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('app-reservation-invite-modal')).not.toBeNull();
    expect(element.querySelector('app-reservation-confirmed')).toBeNull();
  });
});

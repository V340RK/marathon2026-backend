import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, map, Observable, of, switchMap } from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { CardReservationComponent } from '@app/components/reservations/card-reservation/card-reservation.component';
import { TimeSlotsModalComponent } from '@app/components/tables/time-slots-modal/time-slots-modal.component';
import { WaiterMakeReservationComponent } from '@app/components/tables/make-reservation-waiter/waiter-make-reservation/waiter-make-reservation.component';
import { CreateOrderRequest } from '@app/models/cart/create-order-request.model';
import {
  Reservation,
  ReservationCustomer,
  ReservationStatusCustomer,
} from '@app/models/reservations/reservation.model';
import { SelectOption } from '@app/models/shared/select-option.model';
import { TableFilter } from '@app/models/tables/table-filter.model';
import { Table } from '@app/models/tables/table.model';
import { TimeRange } from '@app/models/tables/time-range.model';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { TableService } from '@app/services/table/table.service';
import { ToastService } from '@app/services/toast/toast.service';
import { UsersService } from '@app/services/users/users.service';
import { WaitersService } from '@app/services/waiters/waiters.service';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { HeaderComponent } from '@app/shared/custom-components/header/header.component';
import {
  formatTime,
  generateDateOptions,
  generateTimeOptions,
  revertFormatTime,
} from '@app/shared/helpers/date-helpers';

import { FeedbackQrModalComponent } from './feedback-qr-modal/feedback-qr-modal.component';
import {LocationsService} from '@app/services/locations/locations.service';

interface ReservationFilters {
  date: string;
  time: string;
  tableId?: string;
}

interface ReservationCardViewModel {
  reservation: ReservationCustomer;
  selectedTableLabel: string;
  availableTableOptions: string[];
  availableTableOptionsMap: Map<string, string>;
}

const ANY_TABLE_OPTION: SelectOption = {
  text: 'Any Table',
  value: '',
};

@Component({
  selector: 'app-waiter-dashboard',
  imports: [
    CustomSelectComponent,
    CardReservationComponent,
    HeaderComponent,
    WaiterMakeReservationComponent,
    TimeSlotsModalComponent,
    CustomButtonComponent,
    FeedbackQrModalComponent,
  ],
  templateUrl: './waiter-dashboard.component.html',
  styleUrl: './waiter-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true
})
export class WaiterDashboardComponent implements OnInit {
  private readonly authService = inject(AuthenticationService);
  private readonly reservationService = inject(ReservationService);
  private readonly tableService = inject(TableService);
  private readonly ordersService = inject(PreOrdersService);
  private readonly usersService = inject(UsersService);
  private readonly waiterService = inject(WaitersService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly locationsService = inject(LocationsService);

  readonly firstName = signal('');
  readonly lastName = signal('');
  readonly role = signal('');
  readonly waiterName = computed(() => `${this.firstName()} ${this.lastName()}`.trim());

  readonly reservations = signal<ReservationCustomer[]>([]);
  readonly reservationsLoaded = signal(false);
  readonly tables = signal<Table[]>([]);

  readonly dateOptions = signal<string[]>([]);
  readonly timeOptions = signal<string[]>([]);
  readonly tableOptions = signal<SelectOption[]>([ANY_TABLE_OPTION]);

  readonly selectedDate = signal('');
  readonly selectedTime = signal('');
  readonly selectedTableId = signal<string | undefined>(undefined);

  private readonly appliedFilters = signal<ReservationFilters>({
    date: '',
    time: '',
    tableId: undefined,
  });

  readonly selectedTableOption = computed(() => {
    const tableId = this.selectedTableId();
    return this.tableOptions().find((option) => option.value === (tableId ?? ''));
  });

  readonly filteredReservations = computed(() => {
    const filters = this.appliedFilters();
    let result = this.reservations();

    if (filters.date) {
      const filterDate = new Date(filters.date);
      result = result.filter((reservation) =>
        this.isSameLocalDate(new Date(reservation.startTime), filterDate)
      );
    }

    if (filters.time && filters.date) {
      const filterTimestamp = new Date(revertFormatTime(filters.time, filters.date)).getTime();
      result = result.filter(
        (reservation) => new Date(reservation.startTime).getTime() >= filterTimestamp
      );
    }

    if (filters.tableId) {
      result = result.filter((reservation) => reservation.tableId === filters.tableId);
    }

    return result;
  });

  readonly reservationCards = computed<ReservationCardViewModel[]>(() => {
    const tables = this.tables();

    return this.filteredReservations().map((reservation) => {
      const availableTables = tables.filter((table) => table.id !== reservation.tableId);
      const currentTable = tables.find((table) => table.id === reservation.tableId);

      return {
        reservation,
        selectedTableLabel:
          currentTable != null
            ? `Table ${currentTable.tableNumber}`
            : reservation.tableName || 'Table (Unknown)',
        availableTableOptions: availableTables.map((table) => `Table ${table.tableNumber}`),
        availableTableOptionsMap: new Map(
          availableTables.map((table) => [`Table ${table.tableNumber}`, table.id])
        ),
      };
    });
  });

  readonly filteredReservationsLabel = computed(() => {
    const filters = this.appliedFilters();
    const count = this.filteredReservations().length;
    const date = filters.date || 'any date';
    const time = filters.time || 'any time';

    return `You have ${count} reservation${count === 1 ? '' : 's'} for ${date}, ${time}`;
  });

  readonly assignedLocationId = signal('');
  readonly assignedLocationName = signal('');

  readonly showMakeReservationModal = signal(false);
  readonly showPostponeModal = signal(false);
  readonly reservationToPostpone = signal<ReservationCustomer | undefined>(undefined);
  readonly tableForPostpone = signal<Table | undefined>(undefined);
  readonly postponeDate = signal('');

  readonly showQr = signal(false);
  readonly qrLocationId = signal('');
  readonly qrReservationId = signal('');
  readonly isNewFeedback = signal(true);

  readonly statusEnum: Record<number, { class: string; label: string }> = {
    [ReservationStatusCustomer.Reserved]: { class: 'reserved', label: 'Reserved' },
    [ReservationStatusCustomer.Cancelled]: { class: 'canceled', label: 'Cancelled' },
    [ReservationStatusCustomer.InProgress]: { class: 'in-progress', label: 'In Progress' },
    [ReservationStatusCustomer.Finished]: { class: 'finished', label: 'Finished' },
  };

  ngOnInit(): void {
    const token = this.authService.getToken();
    const user = this.authService.getUser();

    if (!token || !user) {
      this.toast.error('Your session has expired. Please sign in again.', 'Authentication');
      this.reservationsLoaded.set(true);
      return;
    }

    this.firstName.set(user.firstName ?? '');
    this.lastName.set(user.lastName ?? '');
    this.role.set(user.role ?? '');
    this.dateOptions.set(generateDateOptions());

    this.loadReservations();
    this.loadAssignedLocation(user.id);
  }

  onDateSelected(date: string): void {
    this.selectedDate.set(date);
    this.selectedTime.set('');
    this.timeOptions.set(generateTimeOptions(date));
  }

  onTimeSelected(time: string): void {
    this.selectedTime.set(time);
  }

  onTableSelected(tableId: string): void {
    this.selectedTableId.set(tableId || undefined);
  }

  applyFilters(): void {
    this.appliedFilters.set({
      date: this.selectedDate(),
      time: this.selectedTime(),
      tableId: this.selectedTableId(),
    });
  }

  onOpenMakeReservationModal(): void {
    this.showMakeReservationModal.set(true);
  }

  onCloseMakeReservationModal(): void {
    this.showMakeReservationModal.set(false);
  }

  closeQr(): void {
    this.showQr.set(false);
  }

  onSendAnonymousFeedback(reservation: ReservationCustomer): void {
    this.qrReservationId.set(reservation.id);
    this.isNewFeedback.set(
      reservation.feedback?.cuisineExperience == null && reservation.feedback?.service == null
    );

    if (!reservation.waiterId) {
      this.qrLocationId.set(this.assignedLocationId());
      this.showQr.set(true);
      return;
    }

    this.waiterService
      .getWaiterLocationId(reservation.waiterId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (locationId) => {
          this.qrLocationId.set(locationId);
          this.showQr.set(true);
        },
        error: (error) => {
          console.error('Could not load waiter location:', error);
          this.toast.error(
            'Could not create the feedback QR code.',
            'Feedback'
          );
        },
      });
  }

  onMakeReservation(event: {
    tableId: string;
    timeRange: TimeRange;
    guests: number;
    customerEmail: string | null;
  }): void {
    const { tableId, timeRange, guests, customerEmail } = event;

    if (!tableId || !timeRange?.start || !timeRange?.end || guests < 1) {
      this.toast.error('Please complete all reservation fields.', 'Reservation');
      return;
    }

    this.reservationService
      .makeReservation$(tableId, timeRange, guests, customerEmail)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reservation) => {
          this.reservations.update((items) => [
            ...items,
            this.mapToReservationCustomer(reservation),
          ]);
          this.showMakeReservationModal.set(false);
          this.toast.success('New reservation has been created successfully.', 'Success');
        },
        error: () => this.toast.error('The reservation could not be created.', 'Reservation'),
      });
  }

  cancelReservation(id: string): void {
    this.reservationService
      .cancelReservation$(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.error) {
            this.toast.error(response.error, 'Reservation');
            return;
          }

          this.reservations.update((items) =>
            items.map((reservation) =>
              reservation.id === id
                ? { ...reservation, status: ReservationStatusCustomer.Cancelled }
                : reservation
            )
          );
          this.toast.success('Reservation has been cancelled.', 'Success');
        },
        error: () => this.toast.error('The reservation could not be cancelled.', 'Reservation'),
      });
  }

  onReservationTableChanged(event: { reservationId: string; newTableId: string }): void {
    this.tableService
      .changeReservationTable$(event.reservationId, event.newTableId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.reservations.update((items) =>
            items.map((reservation) =>
              reservation.id === updated.id
                ? { ...reservation, tableId: updated.tableId }
                : reservation
            )
          );
          this.toast.success('Table updated successfully.', 'Success');
        },
        error: () => this.toast.error('The table could not be updated.', 'Reservation'),
      });
  }

  onReservationStatusChanged(event: { id: string; newStatus: number }): void {
    this.reservationService
      .changeReservationStatus$(event.id, event.newStatus)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.reservations.update((items) =>
            items.map((reservation) =>
              reservation.id === updated.id
                ? { ...reservation, status: updated.status as ReservationStatusCustomer }
                : reservation
            )
          );
          this.toast.success('Reservation status updated successfully.', 'Success');
        },
        error: () =>
          this.toast.error('The reservation status could not be updated.', 'Reservation'),
      });
  }

  onPostponeReservation(reservation: ReservationCustomer): void {
    if (!reservation.tableId) {
      this.toast.error('This reservation has no table.', 'Reservation');
      return;
    }

    const reservationStart = new Date(reservation.startTime);
    const date = new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(reservationStart);
    const time = formatTime(reservationStart.toISOString());

    this.tableService
      .getTableTimeSlots(reservation.tableId, date, time)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (timeRanges) => {
          const knownTable = this.tables().find((table) => table.id === reservation.tableId);

          this.reservationToPostpone.set(reservation);
          this.postponeDate.set(date);
          this.tableForPostpone.set({
            id: reservation.tableId,
            tableNumber: knownTable?.tableNumber ?? 0,
            capacity: knownTable?.capacity ?? reservation.guestCount,
            locationId: knownTable?.locationId ?? this.assignedLocationId(),
            timeRanges,
          });
          this.showPostponeModal.set(true);
        },
        error: () => this.toast.error('Available time slots could not be loaded.', 'Reservation'),
      });
  }

  closePostponeModal(): void {
    this.showPostponeModal.set(false);
    this.reservationToPostpone.set(undefined);
    this.tableForPostpone.set(undefined);
  }

  onPostponeTimeSelected(event: { table: Table; timeRange: TimeRange }): void {
    const reservation = this.reservationToPostpone();
    if (!reservation) return;

    this.reservationService
      .editReservation$(reservation.id, event.timeRange, reservation.guestCount)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.reservations.update((items) =>
            items.map((item) =>
              item.id === updated.id
                ? { ...item, startTime: updated.startTime, endTime: updated.endTime }
                : item
            )
          );
          this.closePostponeModal();
          this.toast.success('Reservation has been postponed successfully.', 'Success');
        },
        error: () => this.toast.error('The reservation could not be postponed.', 'Reservation'),
      });
  }

  onPreOrderWaiter(order: CreateOrderRequest): void {
    this.ordersService
      .createPreOrder(order)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (createdOrder) => {
          this.reservations.update((items) =>
            items.map((reservation) =>
              reservation.id === order.reservationId
                ? { ...reservation, preOrder: createdOrder }
                : reservation
            )
          );
          this.toast.success('Pre-order created successfully.', 'Success');
        },
        error: () => this.toast.error('The pre-order could not be created.', 'Pre-order'),
      });
  }

  private loadReservations(): void {
    const waiterName = this.waiterName();

    this.reservationService
      .getReservationsForUser$()
      .pipe(
        switchMap((reservations) =>
          reservations.length === 0
            ? of([] as ReservationCustomer[])
            : forkJoin(
              reservations.map((reservation) => this.enrichReservation(reservation, waiterName))
            )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (reservations) => {
          this.reservations.set(reservations);
          this.reservationsLoaded.set(true);
        },
        error: () => {
          this.reservationsLoaded.set(true);
          this.toast.error('Reservations could not be loaded.', 'Reservations');
        },
      });
  }

  private enrichReservation(
    reservation: Reservation,
    waiterName: string
  ): Observable<ReservationCustomer> {
    const feedback$ = this.reservationService.getReservationFeedback(reservation.id).pipe(
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 404) {
          console.error('Could not load reservation feedback', error);
        }
        return of(null);
      })
    );

    const creator$ = reservation.customerId
      ? this.usersService.getUserName(reservation.customerId).pipe(
        map((name) => ({ name, role: 'Customer' as const })),
        catchError(() => of({ name: 'Unknown customer', role: 'Customer' as const }))
      )
      : of({ name: waiterName, role: 'Waiter' as const });

    const preOrder$ = this.ordersService.getByReservationId(reservation.id).pipe(
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 404) {
          console.error('Could not load reservation pre-order', error);
        }
        return of(undefined);
      })
    );

    return forkJoin({ feedback: feedback$, creator: creator$, preOrder: preOrder$ }).pipe(
      map(({ feedback, creator, preOrder }) => ({
        ...reservation,
        status: reservation.status as unknown as ReservationStatusCustomer,
        restaurantName: reservation.restaurantName ?? '',
        feedback: feedback ?? undefined,
        createdByName: creator.name,
        createdByRole: creator.role,
        preOrder,
        hasSubmittedPreOrder: preOrder != null && (preOrder.status === 1 || preOrder.status === 3),
      }))
    );
  }

  private loadAssignedLocation(waiterId: string): void {
    this.waiterService
      .getWaiterLocationId(waiterId)
      .pipe(
        switchMap((locationId) => {
          if (!locationId) {
            throw new Error('The waiter has no assigned location.');
          }

          const filter: TableFilter = {
            location: locationId,
            guests: 1,
            date: null,
            time: null,
          };

          return forkJoin({
            location: this.locationsService.getLocationById(locationId),
            tables: this.tableService.getAvailableTablesWithoutStartTime$(filter),
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ location, tables }) => {
          this.assignedLocationId.set(location.id);
          this.assignedLocationName.set(location.address);
          this.tables.set(tables);

          this.tableOptions.set([
            ANY_TABLE_OPTION,
            ...tables.map((table) => ({
              text: `Table ${table.tableNumber}`,
              value: table.id,
            })),
          ]);
        },
        error: (error: unknown) => {
          console.error('Failed to load waiter location or tables:', error);
          this.toast.error(
            'The waiter location or tables could not be loaded.',
            'Tables'
          );
        },
      });
  }

  private mapToReservationCustomer(reservation: Reservation): ReservationCustomer {
    return {
      ...reservation,
      status: reservation.status as unknown as ReservationStatusCustomer,
      restaurantName: reservation.restaurantName ?? '',
      feedback: undefined,
      createdByName: this.waiterName(),
      createdByRole: 'Waiter',
    };
  }

  private isSameLocalDate(left: Date, right: Date): boolean {
    return (
      left.getFullYear() === right.getFullYear() &&
      left.getMonth() === right.getMonth() &&
      left.getDate() === right.getDate()
    );
  }
}

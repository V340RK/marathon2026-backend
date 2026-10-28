import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableCardComponent } from '../table-card/table-card.component';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { CustomNumberComponent } from '@app/shared/custom-components/custom-number/custom-number.component';
// import { TimeSlotsModalComponent } from '../time-slots-modal/time-slots-modal.component';
import { TableFilter } from '@app/models/tables/table-filter.model';
import { Table } from '@app/models/tables/table.model';
import { MakeReservationComponent } from '../make-reservation/make-reservation.component';
import { TimeRange } from '@app/models/tables/time-range.model';
import { TableService } from '@app/services/table/table.service';
import {
  addMinutesIso,
  combineDateAndTime,
  generateDateOptions,
  generateTimeOptions,
  minutesBetween,
  revertFormatDate,
} from '@app/shared/helpers/date-helpers';
import { getServerErrorMessage } from '@app/shared/helpers/http-error-helpers';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { LocationsService } from '@app/services/locations/locations.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { ReservationConfirmedComponent } from '@app/components/reservations/reservation-confirmed/reservation-confirmed.component';
import { ReservationInviteModalComponent } from '@app/components/reservations/reservation-invite-modal/reservation-invite-modal.component';
import { ReservationReviewModalComponent } from '@app/components/reservations/reservation-review-modal/reservation-review-modal.component';
import { EditReservationModalComponent } from '@app/components/reservations/edit-reservation-modal/edit-reservation-modal.component';
import { ReservationDraftChanges } from '@app/components/reservations/edit-reservation-modal/models';
import { LocationResponse } from '@app/models/locations/location-response.model';
import { Reservation } from '@app/models/reservations/reservation.model';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { ToastService } from '@app/services/toast/toast.service';

const UNKNOWN_LOCATION_LABEL = 'Location unavailable';
const ALL_LOCATIONS_LABEL = 'All Locations';
const RESERVATION_OVERLAP_MESSAGE =
  "This table isn't free for the whole selected time. Please choose a shorter stay or another slot.";
const RESERVATION_FAILED_MESSAGE = 'Could not create the reservation. Please try again.';

@Component({
  selector: 'app-reservations-page',
  imports: [
    TableCardComponent,
    CustomSelectComponent,
    CustomNumberComponent,
    // TimeSlotsModalComponent,
    MakeReservationComponent,
    ReservationConfirmedComponent,
    ReservationInviteModalComponent,
    ReservationReviewModalComponent,
    EditReservationModalComponent,
  ],
  templateUrl: './tables.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tables.component.scss',
})
export class TablesComponent implements OnInit {
  locations: LocationResponse[] = [];
  locationOptions: string[] = [];
  dateOptions!: string[];
  timeOptions!: string[]; // Initialized later based on selected date
  selectedLocationId?: string;

  private readonly tableService = inject(TableService);
  private readonly locationService = inject(LocationsService);
  private readonly reservationService = inject(ReservationService);
  private readonly authenticationService = inject(AuthenticationService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.selectedLocationId = params['locationId'] ?? undefined;
      console.log('location id: ' + this.selectedLocationId);
    });

    this.locationService
      .getLocations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((locations) => {
        this.locationOptions = [
          ALL_LOCATIONS_LABEL,
          ...locations.map((location) => location.address),
        ];
        this.locations = locations;

        if (this.selectedLocationId != null) {
          const foundLocation = this.locations.find(
            (location) => location.id === this.selectedLocationId
          );

          this.selectedFilter.location = foundLocation?.address || null;
          console.log(this.selectedFilter);
        }
      });
  }
  ngOnInit(): void {
    this.dateOptions = generateDateOptions();
    console.log('Date options:', this.dateOptions);
    this.timeOptions = generateTimeOptions(this.dateOptions[0]);
  }

  selectedFilter: TableFilter = { location: null, date: null, time: null, guests: 1 };
  lastAppliedFilter: TableFilter | null = null;
  filterSummary = '';
  dateError = '';
  timeError = '';

  tables: Table[] = [];
  reservation: Reservation | null = null;
  selectedTable: Table | null = null;
  selectedTimeRange?: TimeRange;
  draftGuests = 1;
  isSubmittingReservation = false;
  isLoading = false;
  private searchSubscription?: Subscription;

  // showTimeSlotsModal = false;
  showReviewModal = false;
  showEditDraftModal = false;
  showReservationConfirmed = false;
  showInviteModal = false;
  showEditReservationModal = false;

  get selectedLocationName(): string {
    if (this.selectedTable) {
      return this.getLocationName(this.selectedTable);
    }

    return this.lastAppliedFilter?.location ?? UNKNOWN_LOCATION_LABEL;
  }

  getLocationName(table: Table): string {
    return (
      this.locations.find((location) => location.id === table.locationId)?.address ??
      this.lastAppliedFilter?.location ??
      UNKNOWN_LOCATION_LABEL
    );
  }

  onLocationSelected(location: string): void {
    this.selectedFilter.location = location === ALL_LOCATIONS_LABEL ? null : location;
  }

  onDateSelected(date: string): void {
    this.selectedFilter.date = date;
    this.dateError = '';
    this.timeOptions = generateTimeOptions(date);
  }

  onTimeSelected(time: string): void {
    this.selectedFilter.time = time;
    this.timeError = '';
  }
  onGuestsFilterChange(guests: number | undefined): void {
    if (guests === undefined) {
      return;
    }

    this.selectedFilter.guests = guests;
  }

  onFilter(): void {
    this.dateError = this.selectedFilter.date ? '' : 'Choose date from the calendar';
    this.timeError = this.selectedFilter.time ? '' : 'Choose time slot';

    if (this.dateError || this.timeError || this.selectedFilter.guests === null) {
      return;
    }

    this.lastAppliedFilter = { ...this.selectedFilter };
    this.filterSummary = this.buildFilterSummary(this.lastAppliedFilter);
    this.tables = [];
    this.loadAvailableTables();
  }

  onShowAllSlots(): void {
    if (!this.lastAppliedFilter) {
      return;
    }

    this.selectedFilter = { ...this.lastAppliedFilter, location: null };
    this.onFilter();
  }

  // onShowAllClick(table: Table): void {
  //   this.showTimeSlotsModal = true;
  //   this.selectedTable = table;
  // }

  // onCloseTimeSlotsModal(): void {
  //   this.showTimeSlotsModal = false;
  // }

  private buildFilterSummary(filter: TableFilter): string {
    const guests = filter.guests ?? 0;
    const guestsLabel = `${guests} ${guests === 1 ? 'guest' : 'guests'}`;

    return [filter.location ?? ALL_LOCATIONS_LABEL, filter.date, filter.time, guestsLabel].join(' · ');
  }

  onTimeSlotsClick(table: Table, timeRange: TimeRange): void {
    this.selectedTimeRange = timeRange;
    this.selectedTable = table;
    this.draftGuests = this.lastAppliedFilter?.guests ?? 1;
    // this.showTimeSlotsModal = false;
    this.showReviewModal = true;
  }

  onCloseReviewModal(): void {
    this.showReviewModal = false;
  }

  onEditReview(): void {
    this.showReviewModal = false;
    this.showEditDraftModal = true;
  }

  onSaveDraft(changes: ReservationDraftChanges): void {
    this.selectedTimeRange = changes.timeRange;
    this.draftGuests = changes.guests;
    this.showEditDraftModal = false;
    this.showReviewModal = true;
    this.toastService.success('Your reservation has been updated successfully.');
  }

  onCloseEditDraftModal(): void {
    this.showEditDraftModal = false;
    this.showReviewModal = true;
  }

  onConfirmReview(): void {
    if (!this.selectedTable || !this.selectedTimeRange || this.isSubmittingReservation) {
      return;
    }

    this.onMakeReservation(this.selectedTable.id, this.selectedTimeRange, this.draftGuests);
  }

  onMakeReservation(tableId: string, timeRange: TimeRange, guests: number): void {
    this.redirectIfNotLoggedIn();
    this.selectedTimeRange = timeRange;

    const date = revertFormatDate(this.selectedFilter.date!);
    const start = combineDateAndTime(date, timeRange.start);
    const newTimeRange: TimeRange = {
      start,
      // derived from the length so a stay that crosses UTC midnight keeps end > start
      end: addMinutesIso(start, minutesBetween(timeRange.start, timeRange.end)),
    };

    this.isSubmittingReservation = true;
    this.reservationService
      .makeReservation$(tableId, newTimeRange, guests, null, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reservation) => {
          this.isSubmittingReservation = false;
          this.reservation = reservation;
          this.showReviewModal = false;
          this.showInviteModal = true;
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmittingReservation = false;
          this.toastService.error(this.buildReservationErrorMessage(error));
        },
      });
  }

  private buildReservationErrorMessage(error: HttpErrorResponse): string {
    const serverMessage = getServerErrorMessage(error);

    if (error.status === 400 && serverMessage.toLowerCase().includes('overlap')) {
      return RESERVATION_OVERLAP_MESSAGE;
    }
    if (error.status === 401) {
      return 'Please sign in to make a reservation.';
    }

    return error.status === 400 && serverMessage ? serverMessage : RESERVATION_FAILED_MESSAGE;
  }

  onCloseInviteModal(): void {
    this.showInviteModal = false;
    this.loadAvailableTables();
  }

  private redirectIfNotLoggedIn() {
    const token = this.authenticationService.getToken();
    if (!token) {
      console.error('User is not authenticated. Cannot make a reservation.');
      this.router.navigate(['/login']);
    }
  }

  onCloseReservationConfirmed(): void {
    this.showReservationConfirmed = false;
    this.loadAvailableTables();
  }

  onEditReservation(): void {
    this.showReservationConfirmed = false;
    this.showEditReservationModal = true;
  }

  onCancelReservation(): void {
    this.redirectIfNotLoggedIn();
    this.reservationService
      .cancelReservation$(this.reservation!.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.reservation = null;
        this.showReservationConfirmed = false;
        this.loadAvailableTables();
      });
  }

  onSubmitEditReservation(timeRange: TimeRange, guests: number): void {
    this.redirectIfNotLoggedIn();
    this.reservationService
      .editReservation$(this.reservation!.id, timeRange, guests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((updatedReservation) => {
        this.reservation = updatedReservation;
        this.showEditReservationModal = false;
        this.showReservationConfirmed = true;
      });

    this.showEditReservationModal = false;
  }

  onCloseEditReservationModal(): void {
    this.showEditReservationModal = false;
  }

  loadAvailableTables(): void {
    if (this.lastAppliedFilter) {
      const filter = {
        ...this.lastAppliedFilter,
        location:
          this.locations.find((loc) => loc.address === this.lastAppliedFilter!.location)?.id ||
          null,
      };

      this.searchSubscription?.unsubscribe();
      this.isLoading = true;
      this.searchSubscription = this.tableService
        .getAvailableTablesWithTimeSlots$(filter)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (tables) => {
            this.tables = tables;
            this.isLoading = false;
          },
          error: () => {
            this.tables = [];
            this.isLoading = false;
            this.toastService.error('Could not load available tables. Please try again.');
          },
        });
    }
  }
}

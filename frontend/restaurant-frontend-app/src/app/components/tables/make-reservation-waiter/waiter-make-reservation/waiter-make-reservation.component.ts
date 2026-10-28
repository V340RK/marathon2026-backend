import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  ChangeDetectionStrategy, inject,
} from '@angular/core';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { CustomNumberComponent } from '@app/shared/custom-components/custom-number/custom-number.component';
import { Table } from '@app/models/tables/table.model';
import { TimeRange } from '@app/models/tables/time-range.model';
import { addAndRoundBookingTime, formatTime } from '@app/shared/helpers/date-helpers';

import { LocationsService } from '@app/services/locations/locations.service';
import { WaitersService } from '@app/services/waiters/waiters.service';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { UserModel } from '@app/auth/models/user.model';
import { TableService } from '@app/services/table/table.service';
import { TableFilter } from '@app/models/tables/table-filter.model';
import { UsersService } from '@app/services/users/users.service';
import { FormsModule } from '@angular/forms';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  Subject,
  switchMap,
} from 'rxjs';
import {ToastService} from '@app/services/toast/toast.service';

@Component({
  selector: 'app-waiter-make-reservation',
  imports: [
    FormsModule,
    CustomButtonComponent,
    CustomSelectComponent,
    CustomNumberComponent,
  ],
  templateUrl: './waiter-make-reservation.component.html',
  styleUrl: './waiter-make-reservation.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: true,
})
export class WaiterMakeReservationComponent implements OnInit {
  public formatTime = formatTime;
  private toast = inject(ToastService);

  title = 'New Reservation';
  guests = 1;

  @Output() closeModal = new EventEmitter<void>();
  @Output() makeReservation = new EventEmitter<{
    tableId: string;
    timeRange: TimeRange;
    guests: number;
    customerEmail: string | null;
  }>();

  user!: UserModel | null;

  locations: { id: string; address: string }[] = [];
  selectedLocationId?: string;
  selectedLocation?: string;

  tables: Table[] = [];
  selectedTable?: Table;

  availableTimeSlots: TimeRange[] = [];
  selectedTimeRange: TimeRange = { start: '', end: '' };
  selectedStartTime = '';
  selectedEndTime = '';

  reservationType: 'visitor' | 'existingUser' = 'visitor';
  searchedUsers: UserModel[] = [];
  searchQuery = '';
  selectedCustomerId: string | null = null;
  selectedCustomerEmail: string | null = null;

  searchQuery$ = new Subject<string>();
  guestsChange$ = new Subject<number>();

  private readonly locationService = inject(LocationsService);
  private readonly waitersService = inject(WaitersService);
  private readonly authenticationService = inject(AuthenticationService);
  private readonly tableService = inject(TableService);
  private readonly usersService = inject(UsersService);


  ngOnInit(): void {
    this.user = this.authenticationService.getUser();

    this.loadLocations();

    this.searchQuery$
      .pipe(
        debounceTime(700),
        distinctUntilChanged(),
        switchMap((query) =>
          query.trim() ? this.usersService.getUsers(query).pipe(catchError(() => of([]))) : of([])
        )
      )
      .subscribe((users) => (this.searchedUsers = users));

    this.guestsChange$.pipe(debounceTime(700), distinctUntilChanged()).subscribe((guests) => {
      this.guests = guests;
      this.loadTables();
    });
  }

  private loadLocations() {
    this.locationService.getLocations().subscribe({
      next: (data) => {
        this.locations = data.map((loc) => ({ id: loc.id, address: loc.address }));

        if (this.user) {
          this.waitersService.getWaiterLocationId(this.user.id).subscribe({
            next: (locationId) => {
              const waiterLocation = this.locations.find(
                (location) => location.id === locationId
              );

              if (waiterLocation) {
                this.selectedLocation = waiterLocation.address;
                this.selectedLocationId = waiterLocation.id;
                this.loadTables();
              }
            },
          });
        }
      },
      error: (err) => console.error('Failed to load locations', err),
    });
  }

  loadTables(): void {
    if (!this.selectedLocationId) {
      return;
    }

    const filter: TableFilter = {
      location: this.selectedLocationId,
      date: null,
      time: null,
      guests: this.guests,
    };

    this.tableService
      .getAvailableTablesWithoutStartTime$(filter)
      .pipe(
        catchError((error) => {
          console.error('Failed to load tables:', error);
          return of([]);
        })
      )
      .subscribe((tables) => {
        this.tables = tables;

        if (tables.length === 0) {
          this.clearSelection();
          return;
        }

        this.selectedTable = tables[0];

        this.loadTimeSlotsForSelectedTable();
      });
  }

  loadTimeSlotsForSelectedTable() {
    if (!this.selectedTable) return;

    const now = new Date();
    const today = now.toISOString().split('T')[0];
    const currentTime = now.toISOString();
    const minimumBookingTime = addAndRoundBookingTime(currentTime);

    this.tableService
      .getTableTimeSlots(this.selectedTable.id, today, formatTime(minimumBookingTime))
      .pipe(catchError(() => of([])))
      .subscribe((slots) => {
        this.availableTimeSlots = slots;

        if (this.availableTimeSlots.length > 0) {
          this.selectedTimeRange = this.availableTimeSlots[0];
          this.selectedStartTime = this.selectedTimeRange.start;
          this.selectedEndTime = this.selectedTimeRange.end;
        } else {
          this.clearTimeSelection();
        }
      });
  }

  onLocationChange(address: string) {
    const selectedLoc = this.locations.find((loc) => loc.address === address);
    if (selectedLoc) {
      this.selectedLocation = address;
      this.selectedLocationId = selectedLoc.id;
      this.loadTables();
    }
  }

  onTableChange(tableName: string): void {
    const tableNumber = tableName.replace('Table ', '');

    const table = this.tables.find(
      (item) => item.tableNumber.toString() === tableNumber
    );

    if (!table) {
      return;
    }

    this.selectedTable = table;
    this.loadTimeSlotsForSelectedTable();
  }

  onStartTimeChange(formattedTime: string) {
    const selectedSlot = this.availableTimeSlots.find(
      (slot) => formatTime(slot.start) === formattedTime
    );

    if (selectedSlot) {
      this.selectedStartTime = selectedSlot.start;
      this.selectedEndTime = selectedSlot.end;
      this.selectedTimeRange = selectedSlot;
    }
  }

  onEndTimeChange(formattedTime: string) {
    const selectedSlot = this.availableTimeSlots.find(
      (slot) => formatTime(slot.end) === formattedTime
    );

    if (selectedSlot) {
      this.selectedEndTime = selectedSlot.end;
      this.selectedTimeRange = {
        start: this.selectedStartTime,
        end: selectedSlot.end,
      };
    }
  }

  onMakeReservation() {
    if (!this.selectedTable || !this.selectedStartTime || !this.selectedEndTime) return;

    const timeRange: TimeRange = {
      start: this.selectedStartTime,
      end: this.selectedEndTime,
    };

    this.makeReservation.emit({
      tableId: this.selectedTable.id,
      timeRange,
      guests: this.guests,
      customerEmail: this.reservationType === 'visitor' ? null : this.selectedCustomerEmail,
    });

    //ask designers
    this.toast.success('Reservation created successfully.', 'Success');
  }

  onGuestsChange(guests: number | undefined): void {
    if (guests === undefined) {
      return;
    }

    this.guestsChange$.next(guests);
  }

  selectUser(user: UserModel) {
    this.selectedCustomerEmail = user.email;
  }

  get locationAddresses(): string[] {
    return this.locations.map((loc) => loc.address);
  }

  get tableNames(): string[] {
    return this.tables.map((t) => `Table ${t.tableNumber}`);
  }

  get startTimeOptions(): string[] {
    return this.availableTimeSlots.map((slot) => formatTime(slot.start));
  }

  get endTimeOptions(): string[] {
    return this.availableTimeSlots.map((slot) => formatTime(slot.end));
  }

  clearSelection() {
    this.selectedTable = undefined;
    this.availableTimeSlots = [];
    this.clearTimeSelection();
  }

  clearTimeSelection() {
    this.selectedTimeRange = { start: '', end: '' };
    this.selectedStartTime = '';
    this.selectedEndTime = '';
  }

  onClose() {
    this.closeModal.emit();
  }
}

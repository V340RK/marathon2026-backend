import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import {
  ChangeDetectorRef,
  Component,
  inject,
  NgZone,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { HeaderComponent } from '@app/shared/custom-components/header/header.component';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';

import { ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import dayjs from 'dayjs';
import { LocationResponse } from '@app/models/locations/location-response.model';
import { LocationsService } from '@app/services/locations/locations.service';
import { ReportsService } from '@app/services/reports/reports.service';
import { CommonModule } from '@angular/common';
import { ReportRow } from '@app/models/reports/reporsts.model';

@Component({
  selector: 'app-reporting-dashboard',
  imports: [
    HeaderComponent,
    CustomSelectComponent,
    ReactiveFormsModule,
    ReactiveFormsModule,
    FormsModule,
    // NgxDaterangepickerBootstrapDirective,
    CustomButtonComponent,
    CommonModule,
  ],
  templateUrl: './reporting-dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './reporting-dashboard.component.scss',
})
export class ReportingDashboardComponent implements OnInit {
  firstName: string | null = null;
  lastName: string | null = null;
  role: string | null = null;
  customerId: string | null = null;
  selectedReportType?: string;
  selectedPeriod?: string;
  reportTypes: string[] = ['Staff performance', 'Sales'];
  // selected: { startDate: Dayjs; endDate: Dayjs } = {
  //   startDate: dayjs(),
  //   endDate: dayjs(),
  // };
  calendarVisible = false;
  currentMonth = dayjs();

  selected: { startDate: dayjs.Dayjs; endDate: dayjs.Dayjs | null } | null = null;

  locale = {
    format: 'MMM D, YYYY',
    displayFormat: 'MMM D, YYYY',
    firstDay: 0,
    applyLabel: 'Apply',
    cancelLabel: 'Cancel',
    daysOfWeek: ['M', 'T', 'W', 'T', 'F', 'S', 'S'],
    monthNames: [
      'January',
      'February',
      'March',
      'April',
      'May',
      'June',
      'July',
      'August',
      'September',
      'October',
      'November',
      'December',
    ],
    customRangeLabel: '',
  };

  // pickerOptions = {
  //   showDropdowns: false,
  //   alwaysShowCalendars: true,
  //   singleDatePicker: false,
  //   showCustomRangeLabel: false,
  //   linkedCalendars: false,
  //   autoApply: true,
  //   isInvalidDate: () => false,
  //   isCustomDate: () => false,
  //   showWeekNumbers: false,
  //   customRangeDirection: true,
  // };

  authenticationService = inject(AuthenticationService);
  locationsService = inject(LocationsService);
  reportsService = inject(ReportsService);

  locations: LocationResponse[] = [];
  selectedLocationId?: string;
  selectedLocationAddress?: string;
  weekDays: string[] = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];

  reportData: ReportRow[] = [];
  downloadOptions = ['Download in PDF', 'Download in Excel', 'Download in CSV'];
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  async ngOnInit(): Promise<void> {
    const token = this.authenticationService.getToken();

    if (!token) {
      console.warn('No token found in sessionStorage');
      return;
    }

    try {
      const user = this.authenticationService.getUser();
      if (!user) {
        console.warn('No user found in localStorage');
        return;
      }

      const { firstName, lastName, role } = user;
      this.firstName = firstName;
      this.lastName = lastName;
      this.role = role;

      this.customerId = user.id;
    } catch (error) {
      console.error('Error loading user profile:', error);
    }

    this.locationsService.getLocations().subscribe({
      next: (locations) => {
        this.locations = locations;
      },
      error: (err) => console.error('Failed to load locations:', err),
    });
  }

  resetDateRange(): void {
    this.selected = null;
  }

  onReportTypeSelected(option: string) {
    this.selectedReportType = option;
    this.reportData = [];
    console.log('Report type selected:', option);
  }

  onDatesUpdated(event: unknown): void {
    if (
      event &&
      typeof event === 'object' &&
      'startDate' in event &&
      'endDate' in event &&
      event['startDate'] &&
      event['endDate']
    ) {
      const { startDate, endDate } = event as { startDate: Date; endDate: Date };
      this.selected = {
        startDate: dayjs(startDate),
        endDate: dayjs(endDate),
      };
    } else {
      this.selected = null;
    }
  }

  getPeriodPlaceholder(): string {
    if (this.selected?.startDate?.isValid() && this.selected?.endDate?.isValid()) {
      return `${this.selected.startDate.format('MMM D, YYYY')} – ${this.selected.endDate.format('MMM D, YYYY')}`;
    }
    return 'Select period';
  }

  onLocationSelected(address: string): void {
    const selected = this.locations.find((loc) => loc.address === address);
    this.selectedLocationId = selected?.id;
    this.reportData = [];
    this.selectedLocationAddress = selected?.address;
    console.log('Selected locationId:', this.selectedLocationId);
  }

  get locationAddresses(): string[] {
    return this.locations.map((loc) => loc.address);
  }

  getSelectedLocationAddress(): string | undefined {
    return this.locations.find((loc) => loc.id === this.selectedLocationId)?.address;
  }

  genetateReportRequest(): void {
    if (
      !this.selectedReportType ||
      !this.selectedLocationId ||
      !this.selected?.startDate ||
      !this.selected?.endDate
    ) {
      console.warn('Missing data to generate report');
      return;
    }

    const type = this.selectedReportType === 'Staff performance' ? 0 : 1;
    const startDate = this.selected.startDate.format('YYYY-MM-DD');
    const endDate = this.selected.endDate.format('YYYY-MM-DD');
    const locationId = this.selectedLocationId;

    this.reportsService.getReportData(type, startDate, endDate, locationId).subscribe({
      next: (data) => {
        this.reportData = data;
      },
      error: (err) => {
        console.error('Failed to fetch report data:', err);
      },
    });
  }

  onDownloadOptionSelected(option: string): void {
    if (
      !this.selectedReportType ||
      !this.selectedLocationId ||
      !this.selected?.startDate ||
      !this.selected?.endDate
    ) {
      console.warn('Report type or location not selected.');
      return;
    }

    const type = this.selectedReportType === 'Staff performance' ? 0 : 1;
    const startDate = this.selected.startDate.format('YYYY-MM-DD');
    const endDate = this.selected.endDate.format('YYYY-MM-DD');
    const locationId = this.selectedLocationId;

    switch (option) {
      case 'Download in PDF':
        this.reportsService.downloadReportPdf(type, startDate, endDate, locationId);
        break;
      case 'Download in Excel':
        this.reportsService.downloadReportExcel(type, startDate, endDate, locationId);
        break;
      case 'Download in CSV':
        this.reportsService.downloadReportCsv(type, startDate, endDate, locationId);
        break;
      default:
        console.warn('Unknown download option:', option);
    }
  }

  toggleCalendar(): void {
    this.calendarVisible = !this.calendarVisible;
    this.cdr.detectChanges();
  }

  onDayClicked(date: Date): void {
    const clickedDay = dayjs(date);

    if (!this.selected || (this.selected.startDate && this.selected.endDate)) {
      this.selected = { startDate: clickedDay, endDate: null };
    } else if (clickedDay.isBefore(this.selected.startDate, 'day')) {
      this.selected = { startDate: clickedDay, endDate: null };
    } else {
      this.selected.endDate = clickedDay;
      this.reportData = [];

      setTimeout(() => {
        this.calendarVisible = false;
        this.cdr.detectChanges();
      });
    }
    console.log('Selected start date:', this.selected?.startDate?.format('YYYY-MM-DD'));
    console.log('Selected end date:', this.selected?.endDate?.format('YYYY-MM-DD'));

    this.cdr.detectChanges();
  }

  isSelected(date: Date): boolean {
    const day = dayjs(date);
    if (!this.selected) return false;

    const { startDate, endDate } = this.selected;
    const isStart = day.isSame(startDate, 'day');
    const isEnd = endDate !== null && day.isSame(endDate, 'day');

    return isStart || isEnd;
  }

  isInRange(date: Date): boolean {
    const day = dayjs(date);
    if (!this.selected?.startDate || !this.selected?.endDate) return false;

    return (
      day.isAfter(this.selected.startDate, 'day') && day.isBefore(this.selected.endDate, 'day')
    );
  }

  isInvalidDate(date: Date): boolean {
    return Number.isNaN(date.getTime());
  }

  getDaysInMonth(): Date[] {
    const year = this.currentMonth.year();
    const month = this.currentMonth.month();
    const days: Date[] = [];

    const firstDayOfWeek = new Date(year, month, 1).getDay();
    const leadingEmpty = firstDayOfWeek === 0 ? 6 : firstDayOfWeek - 1;

    for (let i = 0; i < leadingEmpty; i++) {
      days.push(new Date(NaN));
    }

    const total = new Date(year, month + 1, 0).getDate();
    for (let d = 1; d <= total; d++) {
      days.push(new Date(year, month, d));
    }

    return days;
  }

  previousMonth(): void {
    this.currentMonth = this.currentMonth.subtract(1, 'month');
  }

  nextMonth(): void {
    this.currentMonth = this.currentMonth.add(1, 'month');
  }
}

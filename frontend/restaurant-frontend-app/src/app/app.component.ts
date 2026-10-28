import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {} from './auth/components/register/register.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'restaurant-frontend-app';

  // constructor(private reportService: ReportsService) {
  //   this.reportService.downloadReportPdf(0, '2025-07-04', '2025-07-07', '95e66d7f-b38a-423b-a052-ca0fb4092e45');
  //   this.reportService.downloadReportExcel(0, '2025-07-04', '2025-07-07', '95e66d7f-b38a-423b-a052-ca0fb4092e45');
  //   this.reportService.downloadReportCsv(0, '2025-07-04', '2025-07-07', '95e66d7f-b38a-423b-a052-ca0fb4092e45');
  // }
}

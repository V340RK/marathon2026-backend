import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReportRow } from '@app/models/reports/reporsts.model';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly baseUrl = inject(API_BASE_URL) + '/reports';
  private readonly http = inject(HttpClient);

  // params example: (0, '2025-02-04', '2025-02-11', 'locationId')
  downloadReportCsv(type: number, startDate: string, endDate: string, locationId: string) {
    const url = `${this.baseUrl}/download?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}&format=csv`;

    this.http.get(url, { responseType: 'text' }).subscribe(
      (csvText) => {
        const blob = new Blob([csvText], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = `report_${startDate}_${endDate}.csv`;
        link.click();
        window.URL.revokeObjectURL(link.href);
      },
      (error) => {
        console.error('Error downloading Excel report:', error);
      }
    );
  }

  downloadReportPdf(type: number, startDate: string, endDate: string, locationId: string) {
    const url = `${this.baseUrl}/download?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}&format=pdf`;

    this.http.get(url, { responseType: 'blob' }).subscribe(
      (blob) => {
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = `report_${startDate}_${endDate}.pdf`;
        link.click();
        window.URL.revokeObjectURL(link.href);
      },
      (error) => {
        console.error('Error downloading PDF report:', error);
      }
    );
  }

  downloadReportExcel(type: number, startDate: string, endDate: string, locationId: string) {
    const url = `${this.baseUrl}/download?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}&format=excel`;

    this.http.get(url, { responseType: 'blob' }).subscribe(
      (blob) => {
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = `report_${startDate}_${endDate}.xls`;
        link.click();
        window.URL.revokeObjectURL(link.href);
      },
      (error) => {
        console.error('Error downloading Excel report:', error);
      }
    );
  }

  getReportData(type: number, startDate: string, endDate: string, locationId: string) {
    const url = `${this.baseUrl}/generate?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}`;
    return this.http.get<ReportRow[]>(url);
  }

  // for lambda
  // downloadReportExcel(type: number, startDate: string, endDate: string, locationId: string) {
  //   const url = `${this.baseUrl}/download?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}&format=excel`;

  //   this.http.get(url, { responseType: 'text' }).subscribe(base64 => {
  //     const binary = atob(base64);
  //     const len = binary.length;
  //     const bytes = new Uint8Array(len);
  //     for (let i = 0; i < len; i++) {
  //       bytes[i] = binary.charCodeAt(i);
  //     }

  //     const blob = new Blob([bytes], { type: 'application/vnd.ms-excel' });
  //     const link = document.createElement('a');
  //     link.href = window.URL.createObjectURL(blob);
  //     link.download = `report_${startDate}_${endDate}.xls`;
  //     link.click();
  //     window.URL.revokeObjectURL(link.href);
  //   },
  //     error => {
  //         console.error('Error downloading Excel report:', error);
  //     });
  // }

  // downloadReportPdf(type: number, startDate: string, endDate: string, locationId: string) {
  //   const url = `${this.baseUrl}/download?type=${type}&startDate=${startDate}&endDate=${endDate}&locationId=${locationId}&format=pdf`;

  //   this.http.get(url, { responseType: 'text' }).subscribe(base64 => {
  //     const binary = atob(base64);
  //     const len = binary.length;
  //     const bytes = new Uint8Array(len);
  //     for (let i = 0; i < len; i++) {
  //       bytes[i] = binary.charCodeAt(i);
  //     }

  //     const blob = new Blob([bytes], { type: 'application/pdf' });
  //     const link = document.createElement('a');
  //     link.href = window.URL.createObjectURL(blob);
  //     link.download = `report_${startDate}_${endDate}.pdf`;
  //     link.click();
  //     window.URL.revokeObjectURL(link.href);
  //   },
  //     error => {
  //         console.error('Error downloading Excel report:', error);
  //     });
  // }
}

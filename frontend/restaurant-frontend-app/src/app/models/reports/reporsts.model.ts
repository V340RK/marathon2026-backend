export interface ReportRow {
  locationName: string;
  locationId?: string;
  waiterName: string;
  waiterEmail: string;
  waiterId?: string;
  startDate: string;
  endDate: string;
  workingHours: number;
  totalOrders: number;
  ordersProcessed: number;
  deltaTotalOrders: number;
  ordersDelta: string;
  avgServiceFeedback: number;
  deltaAvgServiceFeedback?: number;
  minServiceFeedback?: number;
  reportType?: number;
  userId?: string;
  revenue?: number;
  avgCuisineFeedback?: number;
  minCuisineFeedback?: number;
  deltaRevenue?: number;
  deltaAvgCuisineFeedback?: number;
}

import { TimeRange } from '@app/models/tables/time-range.model';

export interface ReservationDraftChanges {
  timeRange: TimeRange;
  guests: number;
}

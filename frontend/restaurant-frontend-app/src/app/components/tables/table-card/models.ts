import { TimeRange } from '@app/models/tables/time-range.model';

export interface HourSlot {
  label: string;
  timeRange: TimeRange | null;
}

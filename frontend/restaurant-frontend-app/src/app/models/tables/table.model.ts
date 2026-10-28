import { TimeRange } from './time-range.model';

export interface Table {
  id: string;
  tableNumber: number;
  locationId: string;
  capacity: number;
  timeRanges: TimeRange[];
}

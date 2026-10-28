import { ReviewType } from '../enums/review-type.model';

export interface UpdateFeedbackRequest {
  reservationId: string;
  userId?: string;
  comment?: string;
  rating: number;
  type: ReviewType;
}

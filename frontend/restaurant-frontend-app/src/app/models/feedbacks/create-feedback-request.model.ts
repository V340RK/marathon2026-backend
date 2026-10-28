import { ReviewType } from '../enums/review-type.model';

export interface CreateFeedbackRequest {
  reservationId: string;
  userId?: string;
  comment?: string;
  rating: number;
  type: ReviewType;
  token?: string; // Optional token for anonymous feedback
}

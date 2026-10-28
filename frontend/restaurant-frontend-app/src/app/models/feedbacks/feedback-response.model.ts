import { FeedbackResponseItem } from './feedback-response-item.model';

export interface FeedbackResponse {
  items: FeedbackResponseItem[];
  totalPages: number;
  currentPage: number;
}

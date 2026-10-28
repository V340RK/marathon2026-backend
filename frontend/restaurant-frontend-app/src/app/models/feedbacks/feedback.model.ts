export interface Feedback {
  userId: string;
  locationId: string;
  reservationId: string;
  username: string;
  rating: number;
  comment: string;
  createdAt: string;
  type: string;
}

export interface FeedbackDraftItem {
  rating: number;
  comment: string;
}

export interface FeedbackUpdatedEvent {
  reservationId: string;
  feedback: {
    service: FeedbackDraftItem | null;
    cuisineExperience: FeedbackDraftItem | null;
  };
}

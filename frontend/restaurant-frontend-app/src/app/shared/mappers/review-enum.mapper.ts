import { ReviewSort } from '@app/models/enums/review-sort.model';
import { ReviewType } from '@app/models/enums/review-type.model';

export const ReviewSortLabels: Record<ReviewSort, string> = {
  [ReviewSort.TopRatedFirst]: 'Top rated first',
  [ReviewSort.LowRatedFirst]: 'Low rated first',
  [ReviewSort.NewestFirst]: 'Newest first',
  [ReviewSort.OldestFirst]: 'Oldest first',
};

export const ReviewTypeLabels: Record<ReviewType, string> = {
  [ReviewType.Service]: 'Service',
  [ReviewType.CuisineExperience]: 'Cuisine experience',
};

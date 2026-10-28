import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocationsService } from '@app/services/locations/locations.service';
import { ReviewSortLabels, ReviewTypeLabels } from '@app/shared/mappers/review-enum.mapper';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ReviewType } from '@app/models/enums/review-type.model';
import { ReviewSort } from '@app/models/enums/review-sort.model';
import { LocationResponse } from '@app/models/locations/location-response.model';
import { HeaderComponent } from '@app/shared/custom-components/header/header.component';
import { FeedbacksService } from '@app/services/feedbacks/feedbacks.service';
import { UsersService } from '@app/services/users/users.service';
import { ToastService } from '@app/services/toast/toast.service';
import { FeedbackResponseItem } from '@app/models/feedbacks/feedback-response-item.model';

@Component({
  selector: 'app-location-overview',
  imports: [CommonModule, HttpClientModule, HeaderComponent, RouterLink],
  templateUrl: './location-overview.component.html',
  styleUrl: './location-overview.component.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  providers: [LocationsService],
})
export class LocationOverviewComponent implements OnInit {
  readonly maxStars = 5;

  ReviewType = ReviewType;
  ReviewSort = ReviewSort;

  locationId!: string;
  location?: LocationResponse;
  reviews: FeedbackResponseItem[] = [];
  userImages: Record <string, string> = {};
  selectedType: ReviewType = ReviewType.Service;
  selectedSort: ReviewSort = ReviewSort.TopRatedFirst;

  currentPage = 1;
  totalPages = 1;

  reviewSortValues = Object.values(ReviewSort);
  reviewSortLabels = ReviewSortLabels;
  reviewsLabel = 'Reviews are loading... Please wait.';

  //  reviewTypeValues = Object.values(ReviewType);
  reviewTypeValues: ReviewType[] = [ReviewType.Service, ReviewType.CuisineExperience];

  reviewTypeLabels = ReviewTypeLabels;

  private readonly route = inject(ActivatedRoute);
  private readonly locationService = inject(LocationsService);
  private readonly feedbacksService = inject(FeedbacksService);
  private readonly usersService = inject(UsersService);
  private readonly toastService = inject(ToastService);

  ngOnInit(): void {
    this.locationId = this.route.snapshot.paramMap.get('id')!;

    this.loadLocationData();
    this.loadReviews();
  }

  loadLocationData(): void {
    this.locationService.getLocationById(this.locationId).subscribe({
      next: (data) => {
        this.location = data;
      },
      error: () => {
        this.toastService.error('Failed to load location details');
      },
    });
  }

  loadReviews(): void {
    this.reviews = [];
    this.reviewsLabel = 'Reviews are loading... Please wait.';
    this.feedbacksService
      .getLocationFeedbacks(this.locationId, this.selectedType, this.selectedSort, this.currentPage)
      .subscribe({
        next: (data) => {
          this.reviews = data.items;
          if (this.reviews.length === 0) {
            this.reviewsLabel = 'No reviews of this type found for this location.';
          }
          this.reviews.forEach((review) => {
            if (!this.userImages[review.userId]) {
              this.usersService.getUserProfileImage(review.userId).subscribe({
                next: (imageUrl) => {
                  this.userImages[review.userId] = imageUrl?.presignedUrl ?? 'assets/img/avatar.png';
                },
                error: () => {
                  this.userImages[review.userId] = 'assets/img/avatar.png'; // Fallback image
                },
              });
            }
          });
          this.totalPages = data.totalPages;
        },
        error: () => {
          this.reviewsLabel = 'Failed to load reviews.';
          this.toastService.error('Failed to load reviews');
        },
      });
  }

  onTabChange(type: ReviewType): void {
    this.selectedType = type;

    this.currentPage = 1;
    this.loadReviews();
  }

  onSortChange(target: EventTarget | null): void {
    if (target && target instanceof HTMLSelectElement) {
      const value = target.value;

      this.selectedSort = value as ReviewSort;

      this.currentPage = 1;
      this.loadReviews();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadReviews();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadReviews();
    }
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadReviews();
  }

  getFeedbackUserImage(userId: string): string {
    return this.userImages[userId];
  }

  get visiblePageNumbers(): number[] {
    const groupSize = 3;
    const startPage = Math.floor((this.currentPage - 1) / groupSize) * groupSize + 1;
    const endPage = Math.min(startPage + groupSize - 1, this.totalPages);

    const pages: number[] = [];
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }
}

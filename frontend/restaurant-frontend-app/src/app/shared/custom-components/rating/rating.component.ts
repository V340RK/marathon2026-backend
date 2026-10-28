import {Component, ChangeDetectionStrategy, model} from '@angular/core';

@Component({
  selector: 'app-rating',
  standalone:true,
  imports: [],
  templateUrl: './rating.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './rating.component.scss',
})
export class RatingComponent {
  readonly rating = model(3);
  readonly stars = Array(5).fill(0);
  readonly filledIcon = 'assets/svg/star.svg';
  readonly emptyIcon = 'assets/svg/star-white.svg';

  setRating(value: number) {
    this.rating.set(value);
  }
}

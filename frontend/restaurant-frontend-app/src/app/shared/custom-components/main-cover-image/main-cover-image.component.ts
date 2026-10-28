import {Component, ChangeDetectionStrategy, input} from '@angular/core';

@Component({
  selector: 'app-main-cover-image',
  standalone: true,
  imports: [],
  templateUrl: './main-cover-image.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './main-cover-image.component.scss',
})
export class MainCoverImageComponent {
  readonly src = input.required<string>();
  readonly alt = input('');
  readonly darkened = input(false);
}

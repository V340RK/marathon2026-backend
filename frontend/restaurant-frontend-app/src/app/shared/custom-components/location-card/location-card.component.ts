import {
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import {
  RouterLink,
  RouterLinkActive,
} from '@angular/router';
import { LocationResponse } from '@app/models/locations/location-response.model';

@Component({
  selector: 'app-location-card',
  templateUrl: './location-card.component.html',
  styleUrls: ['./location-card.component.scss'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [RouterLink, RouterLinkActive],
})
export class LocationCardComponent {
  readonly location = input.required<LocationResponse>();
  readonly maxWidth = input('');

  get tablesLabel(): string {
    return this.location().totalCapacity >= 2
      ? 'tables'
      : 'table';
  }
}

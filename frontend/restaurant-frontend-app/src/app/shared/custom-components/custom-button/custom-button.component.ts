import { NgClass } from '@angular/common';
import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'tertiary';

export type ButtonSize =
  | 'small'
  | 'large'
  | 'extra-large';

export type ButtonType =
  | 'button'
  | 'submit'
  | 'reset';

@Component({
  selector: 'app-custom-button',
  standalone: true,
  imports: [NgClass],
  templateUrl: './custom-button.component.html',
  styleUrl: './custom-button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.custom-button-host--full-width]': 'fullWidth()',
  },
})
export class CustomButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('extra-large');
  readonly type = input<ButtonType>('button');

  readonly disabled = input(false, {
    transform: booleanAttribute,
  });

  readonly fullWidth = input(false, {
    transform: booleanAttribute,
  });
}

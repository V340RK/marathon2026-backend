import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  numberAttribute,
  OnInit,
} from '@angular/core';

@Component({
  selector: 'app-custom-number',
  standalone: true,
  imports: [],
  templateUrl: './custom-number.component.html',
  styleUrl: './custom-number.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomNumberComponent implements OnInit {
  readonly iconPath = input('');
  readonly placeholder = input('');

  readonly showIcon = input(true, {
    transform: booleanAttribute,
  });

  readonly minValue = input(1, {
    transform: numberAttribute,
  });

  readonly maxValue = input(10, {
    transform: numberAttribute,
  });

  readonly currentValue = model<number | undefined>(
    undefined,
  );

  readonly canGoUp = computed(() => {
    const value = this.currentValue();

    return (
      value !== undefined &&
      value < this.maxValue()
    );
  });

  readonly canGoDown = computed(() => {
    const value = this.currentValue();

    return (
      value !== undefined &&
      value > this.minValue()
    );
  });

  ngOnInit(): void {
    if (this.currentValue() === undefined) {
      this.currentValue.set(this.minValue());
    }
  }

  increment(): void {
    this.changeValue(1);
  }

  decrement(): void {
    this.changeValue(-1);
  }

  private changeValue(change: number): void {
    const currentValue = this.currentValue();

    if (currentValue === undefined) {
      return;
    }

    const newValue = currentValue + change;

    if (
      newValue < this.minValue() ||
      newValue > this.maxValue()
    ) {
      return;
    }

    this.currentValue.set(newValue);
  }
}

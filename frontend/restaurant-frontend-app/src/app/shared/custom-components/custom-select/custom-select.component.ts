import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { SelectOption } from '@app/models/shared/select-option.model';

@Component({
  selector: 'app-custom-select',
  standalone: true,
  templateUrl: './custom-select.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './custom-select.component.scss',
})
export class CustomSelectComponent {
  readonly iconPath = input('');
  readonly showIcon = input(true);
  readonly placeholder = input('');
  readonly error = input('');
  readonly trailingIconPath = input('');

  readonly options =
    input<readonly (string | SelectOption)[]>([]);

  readonly selectedOption =
    model<string | SelectOption | undefined>(undefined);

  readonly optionSelected = output<string>();

  readonly dropdownOpen = signal(false);

  readonly displayText = computed(() => {
    const selected = this.selectedOption();

    return selected
      ? this.getOptionText(selected)
      : this.placeholder() || 'Select an option';
  });

  selectOption(option: string | SelectOption): void {
    this.selectedOption.set(option);
    this.dropdownOpen.set(false);
    this.optionSelected.emit(this.getOptionValue(option));
  }

  toggleDropdown(): void {
    this.dropdownOpen.update((open) => !open);
  }

  getOptionText(option: string | SelectOption): string {
    return typeof option === 'string'
      ? option
      : option.text || option.value;
  }

  getOptionValue(option: string | SelectOption): string {
    return typeof option === 'string'
      ? option
      : option.value;
  }

  isOptionSelected(option: string | SelectOption): boolean {
    const selected = this.selectedOption();

    return (
      selected !== undefined &&
      this.getOptionValue(option) === this.getOptionValue(selected)
    );
  }
}

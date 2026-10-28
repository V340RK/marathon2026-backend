import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  host: { '(document:keydown.escape)': 'onClose()' },
})
export class ModalComponent {
  readonly title = input('');
  readonly width = input('520px');
  readonly closeOnBackdrop = input(true);

  readonly closed = output<void>();

  onClose(): void {
    this.closed.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (this.closeOnBackdrop() && event.target === event.currentTarget) {
      this.onClose();
    }
  }
}

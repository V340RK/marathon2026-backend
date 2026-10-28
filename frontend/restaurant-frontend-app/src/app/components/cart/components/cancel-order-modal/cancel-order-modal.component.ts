import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';

@Component({
  selector: 'app-cancel-order-modal',
  standalone: true,
  imports: [],
  templateUrl: './cancel-order-modal.component.html',
  styleUrl: './cancel-order-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelOrderModalComponent {
  readonly orderId = input.required<string>();

  readonly canceled = output<void>();
  readonly closed = output<void>();

  cancelOrder(): void {
    this.canceled.emit();
  }

  closeModal(): void {
    this.closed.emit();
  }
}

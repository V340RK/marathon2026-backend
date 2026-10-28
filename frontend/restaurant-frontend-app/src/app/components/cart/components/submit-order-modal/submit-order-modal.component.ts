import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';

import { Order } from '@app/models/orders/order.model';

@Component({
  selector: 'app-submit-order-modal',
  standalone: true,
  imports: [],
  templateUrl: './submit-order-modal.component.html',
  styleUrl: './submit-order-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubmitOrderModalComponent {
  readonly order = input.required<Order>();

  readonly canceled = output<void>();
  readonly edited = output<void>();
  readonly closed = output<void>();

  cancelOrder(): void {
    this.canceled.emit();
  }

  editOrder(): void {
    this.edited.emit();
  }

  closeModal(): void {
    this.closed.emit();
  }
}

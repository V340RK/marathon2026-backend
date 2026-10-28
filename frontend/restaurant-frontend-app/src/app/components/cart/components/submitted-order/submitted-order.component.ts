import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';

import { Order } from '@app/models/orders/order.model';

@Component({
  selector: 'app-submitted-order',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './submitted-order.component.html',
  styleUrl: './submitted-order.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubmittedOrderComponent {
  readonly order = input.required<Order>();

  readonly canceled = output<void>();
  readonly edited = output<void>();

  readonly totalPrice = computed(() =>
    this.order()
      .orderItems.filter((item) => item.state !== 2)
      .reduce(
        (total, item) =>
          total + item.dish.price * item.quantity,
        0,
      ),
  );

  cancelOrder(): void {
    this.canceled.emit();
  }

  editOrder(): void {
    this.edited.emit();
  }
}

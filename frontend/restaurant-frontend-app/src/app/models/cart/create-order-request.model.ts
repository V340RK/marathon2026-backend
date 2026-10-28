import { OrderItem } from '@app/models/dishes/order-item.model';

export interface CreateOrderRequest {
  reservationId: string;
  orderItems: OrderItem[];
}

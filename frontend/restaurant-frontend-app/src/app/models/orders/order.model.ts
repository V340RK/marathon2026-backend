import { OrderItem } from '../dishes/order-item.model';

export interface Order {
  orderId: string;
  reservationAddress: string;
  reservationTime: string;
  reservationId: string;
  orderItems: OrderItem[];
  status: number;
}

export interface WaiterPreOrderState {
  reservationId: string;
  createdByName: string;
  createdByRole: string;
  tableName: string;
}

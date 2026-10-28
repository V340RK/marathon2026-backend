import { DishListResponse } from './dish-list-response.model';

export interface OrderItem {
  dish: DishListResponse;
  quantity: number;
  state: number; // later must be replaced with an enum, for now using numbers for speed
}

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';

export interface CartItem {
  dish: DishListResponse;
  quantity: number;
}

export interface Cart {
  dishes: CartItem[];
}

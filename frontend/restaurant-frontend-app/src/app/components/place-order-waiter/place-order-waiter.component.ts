import {
  Component,
  EventEmitter,
  inject,
  Input,
  OnInit,
  Output,
  ChangeDetectionStrategy,
} from '@angular/core';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { DishSelectCardComponent } from './dish-select-card/dish-select-card.component';
import { OrderItem } from '@app/models/dishes/order-item.model';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { DishesService } from '@app/services/dishes/dishes.service';
import { CreateOrderRequest } from '@app/models/cart/create-order-request.model';
import {ToastService} from '@app/services/toast/toast.service';

@Component({
    selector: 'app-place-order-waiter',
    imports: [CustomButtonComponent, DishSelectCardComponent, ReactiveFormsModule],
    templateUrl: './place-order-waiter.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './place-order-waiter.component.scss',
    standalone: true
})
export class PlaceOrderWaiterComponent implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dishes = inject(DishesService);
  private readonly toast = inject(ToastService);

  @Input() title = 'Place new Order';
  @Input() reservationId = 'RSV-RST14-T3-170625';
  @Input() customerId?: string;

  waiterName = 'Jane Smith';
  customerName = 'John Doe';

  orderItems: OrderItem[] = [];
  dishNameForm!: FormGroup;

  filteredDishes: DishListResponse[] = [];


  @Output() closeModal = new EventEmitter<void>();
  @Output() placeOrder = new EventEmitter<CreateOrderRequest>();

  constructor() {
    const user = this.auth.getUser();
    if (!user) {
      this.toast.error('You must be logged in to place an order.', 'Authentication Error');
      throw new Error('User is not authenticated');
    }

    this.waiterName = `${user.firstName} ${user.lastName ?? ''}`.trim();
  }
  ngOnInit(): void {
    this.dishNameForm = this.formBuilder.group({
      dishName: ['', [Validators.required, Validators.maxLength(100)]],
    });
    this.auth.getUserNameById$(this.customerId ?? 'noid').subscribe({
      next: (name) => {
        this.customerName = name;
      },
      error: (err) => {this.toast.error('Failed to fetch customer name. Please try again later.', 'Error');
        console.error('Error fetching customer name:', err);
        this.customerName = 'Unknown Customer';
      },
    });
  }

  onClose() {
    this.closeModal.emit();
  }

  onRemoveDish(dishId: string) {
    this.orderItems = this.orderItems.filter((item) => item.dish.id !== dishId);
  }

  onQuantityChanged(dishId: string, quantity: number) {
    const item = this.orderItems.find((item) => item.dish.id === dishId);
    if (item) {
      item.quantity = quantity;
    }
  }

  onCreateOrder() {
    if (this.orderItems.length === 0) {
      this.toast.error('You must select at least one dish to place an order.', 'Order Error');
      return;
    }

    this.placeOrder.emit({
      reservationId: this.reservationId,
      orderItems: this.orderItems,
    });
    this.closeModal.emit();
  }

  onDishNameChange() {
    const newName = this.dishNameForm.get('dishName')?.value;
    if (newName && newName.trim().length >= 3) {
      this.dishes.getDishes(undefined, undefined, newName).subscribe({
        next: (dishes) => {
          this.filteredDishes = dishes;
        },
        error: (err) => {
          console.error('Error fetching dishes:', err);
          this.filteredDishes = [];
        },
      });
    } else {
      this.filteredDishes = [];
    }
  }

  onDishSelected(dish: DishListResponse) {
    const existingItem = this.orderItems.find((item) => item.dish.id === dish.id);
    if (existingItem) {
      return;
    } else {
      this.orderItems.push({
        dish: dish,
        quantity: 1,
        state: 0, // Initial state
      });
    }
    this.filteredDishes = [];
    this.dishNameForm.reset();
  }
}

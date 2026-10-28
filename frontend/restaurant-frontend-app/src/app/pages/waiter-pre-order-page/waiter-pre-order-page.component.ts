import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import {
  ChangeDetectorRef,
  Component,
  inject,
  Input,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OrderItem } from '@app/models/dishes/order-item.model';
import { DishCardComponent } from '@app/shared/custom-components/dish-card/dish-card.component';
import { firstValueFrom } from 'rxjs';
import { WaiterPreOrderState } from '@app/models/orders/order.model';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';

@Component({
  selector: 'app-waiter-pre-order-page',
  imports: [DishCardComponent, RouterLink, CustomButtonComponent],
  templateUrl: './waiter-pre-order-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './waiter-pre-order-page.component.scss',
})
export class WaiterPreOrderPageComponent implements OnInit {
  reservationLabel = '';
  orderItems: OrderItem[] = [];

  reservationId = '';
  createdByName = '';
  createdByRole = '';
  tableName = '';
  reservationTime = '';
  @Input() customClass = '';

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  preOrdersService = inject(PreOrdersService);

  ngOnInit(): void {
    const navState = this.router.getCurrentNavigation()?.extras.state as
      | WaiterPreOrderState
      | undefined;
    let data = navState;

    if (!data?.reservationId) {
      const stored = sessionStorage.getItem('waiterPreOrderData');
      if (stored) {
        data = JSON.parse(stored);
        console.log('data', data);
      }
    }

    if (!data?.reservationId) {
      console.warn('Reservation ID not found');
      return;
    }

    this.createdByName = data.createdByName;
    this.createdByRole = data.createdByRole;
    this.tableName = data.tableName;
    this.reservationId = data.reservationId;

    firstValueFrom(this.preOrdersService.getByReservationId(data.reservationId))
      .then((order) => {
        console.log('order', order);
        console.log('ORDER FROM SERVER:', JSON.stringify(order, null, 2));

        this.orderItems = order.orderItems ?? [];
        this.reservationTime = order.reservationTime || '';
        // this.refreshOrder();
      })
      .catch((err) => {
        console.error('Error loading order:', err);
      });
  }

  onRemove(dishId: string) {
    this.orderItems = this.orderItems.filter((item) => item.dish.id !== dishId);
  }

  confirmDish(dishId: string) {
    this.preOrdersService.confirmDishInOrder(this.reservationId, dishId).subscribe({
      next: () => {
        const index = this.orderItems.findIndex((i) => i.dish.id === dishId);
        if (index !== -1) {
          const item = this.orderItems[index];
          const updatedItem: OrderItem = {
            ...item,
            dish: {
              ...item.dish,
              status: 'Confirmed',
            },
            state: 1, // Update state to 'Confirmed'
          };
          this.orderItems.splice(index, 1, updatedItem);
          this.orderItems = [...this.orderItems];
          this.cdr.detectChanges();
          console.log('Updated orderItems:', this.orderItems);
        }
      },
      error: (err) => {
        console.error(err);
      },
    });
  }

  rejectDish(dishId: string) {
    this.preOrdersService.rejectDishInOrder(this.reservationId, dishId).subscribe({
      next: () => {
        const index = this.orderItems.findIndex((i) => i.dish.id === dishId);
        if (index !== -1) {
          const item = this.orderItems[index];
          const updatedItem: OrderItem = {
            ...item,
            dish: {
              ...item.dish,
              status: 'Rejected',
            },
            state: 2, // Update state to 'Rejected'
          };
          this.orderItems.splice(index, 1, updatedItem);
          this.orderItems = [...this.orderItems];
          this.cdr.detectChanges();
          console.log('Dish rejected and updated locally:', updatedItem);
          console.log('Updated orderItems:', this.orderItems);
        }
      },
      error: (err) => {
        console.error('Failed to reject dish:', err);
      },
    });
  }

  // rejectDish(dishId: string) {
  //   this.preOrdersService.rejectDishInOrder(this.reservationId, dishId).subscribe({
  //     next: async () => {
  //       const order = await firstValueFrom(
  //         this.preOrdersService.getByReservationId(this.reservationId)
  //       );
  //       this.orderItems = order.orderItems ?? [];
  //       this.cdr.detectChanges();
  //       console.log('Order reloaded:', this.orderItems);
  //     },
  //     error: (err) => {
  //       console.error('Failed to reject dish:', err);
  //     },
  //   });
  // }

  // confirmDish(dishId: string) {
  //   this.preOrdersService.confirmDishInOrder(this.reservationId, dishId).subscribe({
  //     next: async () => {
  //       const index = this.orderItems.findIndex((i) => i.dish.id === dishId);
  //       if (index !== -1) {
  //         const item = this.orderItems[index];
  //         const updatedItem: OrderItem = {
  //           ...item,
  //           dish: {
  //             ...item.dish,
  //             status: 'Confirmed',
  //           },
  //         };
  //         this.orderItems.splice(index, 1, updatedItem);
  //         this.orderItems = [...this.orderItems];
  //         this.cdr.detectChanges();
  //         console.log('Locally updated orderItems (confirmed):', this.orderItems);
  //       }

  //       const order = await firstValueFrom(
  //         this.preOrdersService.getByReservationId(this.reservationId)
  //       );
  //       this.orderItems = order.orderItems ?? [];
  //       this.cdr.detectChanges();
  //       console.log('Order reloaded (confirmed):', this.orderItems);
  //       console.log('ORDER FROM SERVER:', JSON.stringify(this.orderItems, null, 2));
  //     },
  //     error: (err) => {
  //       console.error(err);
  //     },
  //   });
  // }

  // rejectDish(dishId: string) {
  //   this.preOrdersService.rejectDishInOrder(this.reservationId, dishId).subscribe({
  //     next: async () => {
  //       const index = this.orderItems.findIndex((i) => i.dish.id === dishId);
  //       if (index !== -1) {
  //         const item = this.orderItems[index];
  //         const updatedItem: OrderItem = {
  //           ...item,
  //           dish: {
  //             ...item.dish,
  //             status: 'Rejected',
  //           },
  //         };
  //         this.orderItems.splice(index, 1, updatedItem);
  //         this.orderItems = [...this.orderItems];
  //         this.cdr.detectChanges();
  //         console.log('Locally updated orderItems (rejected):', this.orderItems);
  //       }

  //       const order = await firstValueFrom(
  //         this.preOrdersService.getByReservationId(this.reservationId)
  //       );
  //       this.orderItems = order.orderItems ?? [];
  //       this.cdr.detectChanges();
  //       console.log('Order reloaded (rejected):', this.orderItems);
  //       console.log('ORDER FROM SERVER:', JSON.stringify(this.orderItems, null, 2));
  //     },
  //     error: (err) => {
  //       console.error('Failed to reject dish:', err);
  //     },
  //   });
  // }
}

import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { Order } from '@app/models/orders/order.model';

import { NotSubmittedOrderComponent } from './not-submitted-order.component';

describe('NotSubmittedOrderComponent', () => {
  let component: NotSubmittedOrderComponent;
  let fixture: ComponentFixture<NotSubmittedOrderComponent>;

  const order = {
    orderId: 'order-1',
    status: 0,
    reservationId: 'res-1',
    reservationAddress: 'Test address',
    reservationTime: '2026-09-07T12:15:00Z',
    orderItems: [
      {
        quantity: 2,
        dish: {
          id: 'dish-1',
          price: 10,
        },
      },
    ],
  } as Order;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotSubmittedOrderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(
      NotSubmittedOrderComponent,
    );

    component = fixture.componentInstance;

    fixture.componentRef.setInput('order', order);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit submitted with the order ID', () => {
    spyOn(component.submitted, 'emit');

    component.submit();

    expect(
      component.submitted.emit,
    ).toHaveBeenCalledWith('order-1');
  });

  it('should calculate the total price', () => {
    expect(component.totalPrice()).toBe(20);
  });
});

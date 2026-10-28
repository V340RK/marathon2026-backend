import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { Order } from '@app/models/orders/order.model';

import { SubmittedOrderComponent } from './submitted-order.component';

describe('SubmittedOrderComponent', () => {
  let component: SubmittedOrderComponent;
  let fixture: ComponentFixture<SubmittedOrderComponent>;

  const order = {
    orderId: 'order-1',
    status: 1,
    reservationId: 'res-1',
    reservationAddress: 'Test Address',
    reservationTime: '2026-09-08T12:15:00Z',
    orderItems: [
      {
        quantity: 2,
        state: 1,
        dish: {
          id: 'dish-1',
          price: 10,
        },
      },
      {
        quantity: 1,
        state: 2,
        dish: {
          id: 'dish-2',
          price: 100,
        },
      },
    ],
  } as Order;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmittedOrderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(
      SubmittedOrderComponent,
    );

    component = fixture.componentInstance;

    fixture.componentRef.setInput('order', order);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit canceled when cancelOrder is called', () => {
    spyOn(component.canceled, 'emit');

    component.cancelOrder();

    expect(component.canceled.emit).toHaveBeenCalled();
  });

  it('should emit edited when editOrder is called', () => {
    spyOn(component.edited, 'emit');

    component.editOrder();

    expect(component.edited.emit).toHaveBeenCalled();
  });

  it('should exclude rejected items from total price', () => {
    expect(component.totalPrice()).toBe(20);
  });
});

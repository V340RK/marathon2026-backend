import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { Order } from '@app/models/orders/order.model';

import { SubmitOrderModalComponent } from './submit-order-modal.component';

describe('SubmitOrderModalComponent', () => {
  let component: SubmitOrderModalComponent;
  let fixture: ComponentFixture<SubmitOrderModalComponent>;

  const order = {
    orderId: 'order-1',
    reservationAddress: 'Test address',
    reservationId: 'res-1',
    reservationTime: '2026-09-07T12:15:00Z',
    status: 1,
    orderItems: [],
  } as Order;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmitOrderModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(
      SubmitOrderModalComponent,
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

  it('should emit closed when closeModal is called', () => {
    spyOn(component.closed, 'emit');

    component.closeModal();

    expect(component.closed.emit).toHaveBeenCalled();
  });
});

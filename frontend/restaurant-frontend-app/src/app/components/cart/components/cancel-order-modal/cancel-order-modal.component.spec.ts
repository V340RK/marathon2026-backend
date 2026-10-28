import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { CancelOrderModalComponent } from './cancel-order-modal.component';

describe('CancelOrderModalComponent', () => {
  let component: CancelOrderModalComponent;
  let fixture: ComponentFixture<CancelOrderModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancelOrderModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(
      CancelOrderModalComponent,
    );

    component = fixture.componentInstance;

    fixture.componentRef.setInput(
      'orderId',
      'order-1',
    );

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

  it('should emit closed when closeModal is called', () => {
    spyOn(component.closed, 'emit');

    component.closeModal();

    expect(component.closed.emit).toHaveBeenCalled();
  });
});

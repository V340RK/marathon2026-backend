import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WaiterMakeReservationComponent } from './waiter-make-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('WaiterMakeReservationComponent', () => {
  let component: WaiterMakeReservationComponent;
  let fixture: ComponentFixture<WaiterMakeReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [WaiterMakeReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WaiterMakeReservationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

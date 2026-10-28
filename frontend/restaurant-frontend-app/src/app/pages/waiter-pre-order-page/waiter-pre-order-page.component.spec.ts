import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WaiterPreOrderPageComponent } from './waiter-pre-order-page.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('WaiterPreOrderPageComponent', () => {
  let component: WaiterPreOrderPageComponent;
  let fixture: ComponentFixture<WaiterPreOrderPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [WaiterPreOrderPageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WaiterPreOrderPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

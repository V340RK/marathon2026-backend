import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReservationDashboardComponent } from './reservation-dashboard.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('ReservationDashboardComponent', () => {
  let component: ReservationDashboardComponent;
  let fixture: ComponentFixture<ReservationDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [ReservationDashboardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

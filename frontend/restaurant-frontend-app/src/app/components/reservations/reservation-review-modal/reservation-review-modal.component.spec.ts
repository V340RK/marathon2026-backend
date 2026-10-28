import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReservationReviewModalComponent } from './reservation-review-modal.component';

const TIME_RANGE = {
  start: new Date(2026, 9, 17, 12, 0).toISOString(),
  end: new Date(2026, 9, 17, 14, 0).toISOString(),
};

describe('ReservationReviewModalComponent', () => {
  let fixture: ComponentFixture<ReservationReviewModalComponent>;
  let component: ReservationReviewModalComponent;
  let element: HTMLElement;

  function render(guests = 2, submitting = false): void {
    fixture.componentRef.setInput('locationName', '48 Rustaveli Avenue');
    fixture.componentRef.setInput('date', 'Oct 17, 2026');
    fixture.componentRef.setInput('timeRange', TIME_RANGE);
    fixture.componentRef.setInput('guests', guests);
    fixture.componentRef.setInput('submitting', submitting);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationReviewModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationReviewModalComponent);
    component = fixture.componentInstance;
    element = fixture.nativeElement;
  });

  it('renders the title, location, date, time and guests', () => {
    render();

    const text = element.textContent ?? '';
    expect(element.querySelector('.modal-title')?.textContent).toBe('Review & Confirm Reservation');
    expect(text).toContain('48 Rustaveli Avenue');
    expect(text).toContain('Oct 17, 2026');
    expect(text).toContain('12:00 - 14:00');
    expect(text).toContain('2 people');
  });

  it('uses singular wording for one guest', () => {
    render(1);

    expect(element.textContent).toContain('1 person');
  });

  it('shows the cancellation note', () => {
    render();

    expect(element.querySelector('.note')?.textContent).toBe(
      'You can cancel up to 30 min before your reservation.'
    );
  });

  it('emits edit when Edit is clicked', () => {
    render();
    spyOn(component.edit, 'emit');

    element.querySelector<HTMLButtonElement>('.edit-btn')!.click();

    expect(component.edit.emit).toHaveBeenCalled();
  });

  it('emits confirmReservation when Confirm is clicked', () => {
    render();
    spyOn(component.confirmReservation, 'emit');

    element.querySelector<HTMLButtonElement>('.confirm-btn')!.click();

    expect(component.confirmReservation.emit).toHaveBeenCalled();
  });

  it('disables Confirm while the reservation is being submitted', () => {
    render(2, true);

    expect(element.querySelector<HTMLButtonElement>('.confirm-btn')!.disabled).toBeTrue();
  });

  it('emits closed when the close button is clicked', () => {
    render();
    spyOn(component.closed, 'emit');

    element.querySelector<HTMLButtonElement>('.modal-close')!.click();

    expect(component.closed.emit).toHaveBeenCalled();
  });
});

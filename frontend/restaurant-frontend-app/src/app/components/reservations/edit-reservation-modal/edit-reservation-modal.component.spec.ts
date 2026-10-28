import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Table } from '@app/models/tables/table.model';
import { EditReservationModalComponent } from './edit-reservation-modal.component';
import { ReservationDraftChanges } from './models';

const localIso = (hours: number, minutes = 0): string =>
  new Date(2026, 9, 17, hours, minutes).toISOString();

const TABLE: Table = {
  id: 'table-1',
  tableNumber: 1,
  locationId: 'loc-1',
  capacity: 4,
  timeRanges: [
    { start: localIso(12), end: localIso(13, 30) },
    { start: localIso(13, 45), end: localIso(15, 15) },
  ],
};

describe('EditReservationModalComponent', () => {
  let fixture: ComponentFixture<EditReservationModalComponent>;
  let component: EditReservationModalComponent;
  let element: HTMLElement;
  let saved: ReservationDraftChanges | undefined;

  function render(timeRange = TABLE.timeRanges[0], guests = 2): void {
    fixture.componentRef.setInput('locationName', '48 Rustaveli Avenue');
    fixture.componentRef.setInput('date', 'Oct 17, 2026');
    fixture.componentRef.setInput('table', TABLE);
    fixture.componentRef.setInput('timeRange', timeRange);
    fixture.componentRef.setInput('guests', guests);
    fixture.detectChanges();
  }

  function clickSave(): void {
    element.querySelector<HTMLButtonElement>('.save-btn')!.click();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditReservationModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(EditReservationModalComponent);
    component = fixture.componentInstance;
    element = fixture.nativeElement;
    saved = undefined;
    component.saveChanges.subscribe((changes) => (saved = changes));
  });

  it('renders the title and the location, table and date subtitle', () => {
    render();

    expect(element.querySelector('.modal-title')?.textContent).toBe('Edit Reservation');
    expect(element.querySelector('.subtitle')?.textContent?.trim()).toBe(
      '48 Rustaveli Avenue · Table 1 · Oct 17, 2026'
    );
  });

  it('preselects the current start time, guests and the slot length as duration', () => {
    render();

    const text = element.textContent ?? '';
    expect(text).toContain('12:00');
    expect(text).toContain('1.5 hours');
    expect(element.querySelector('.value')?.textContent?.trim()).toBe('2');
  });

  it('shows the call-us note', () => {
    render();

    expect(element.querySelector('.note')?.textContent).toContain('+123 445 678 8990');
  });

  it('saves the unchanged draft when nothing is edited', () => {
    render();

    clickSave();

    expect(saved).toEqual({ timeRange: TABLE.timeRanges[0], guests: 2 });
  });

  it('computes the end from the chosen start and duration', () => {
    render();

    component.onStartSelected(TABLE.timeRanges[1].start);
    component.onDurationSelected('180');
    clickSave();

    expect(saved?.timeRange).toEqual({
      start: localIso(13, 45),
      end: localIso(16, 45),
    });
  });

  it('saves the changed number of guests', () => {
    render();

    component.onGuestsChange(3);
    clickSave();

    expect(saved?.guests).toBe(3);
  });

  it('does not let guests exceed the table capacity', () => {
    render(TABLE.timeRanges[0], 4);

    const increase = element.querySelector<HTMLButtonElement>('button[aria-label="Increase value"]');
    expect(increase!.disabled).toBeTrue();
  });

  it('offers stays from 1.5 to at most 3 hours', () => {
    render();

    expect(component['durationOptions'].map((option) => option.text)).toEqual([
      '1.5 hours',
      '2 hours',
      '2.5 hours',
      '3 hours',
    ]);
  });

  it('snaps a slot shorter than the minimum to the closest duration option', () => {
    render({ start: localIso(12), end: localIso(12, 50) });

    clickSave();

    expect(saved?.timeRange.end).toBe(localIso(13, 30));
  });

  it('keeps the current start in the time options when it is not in the table ranges', () => {
    render({ start: localIso(10), end: localIso(11, 30) });

    clickSave();

    expect(saved?.timeRange.start).toBe(localIso(10));
    expect(element.textContent).toContain('10:00');
  });

  it('emits closed when the close button is clicked', () => {
    render();
    spyOn(component.closed, 'emit');

    element.querySelector<HTMLButtonElement>('.modal-close')!.click();

    expect(component.closed.emit).toHaveBeenCalled();
  });
});

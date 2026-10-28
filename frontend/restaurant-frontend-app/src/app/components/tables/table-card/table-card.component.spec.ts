import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TableCardComponent } from './table-card.component';
import { Table } from '@app/models/tables/table.model';

describe('TableCardComponent', () => {
  let component: TableCardComponent;
  let fixture: ComponentFixture<TableCardComponent>;

  const slotStart = (hour: number): string => new Date(2026, 9, 17, hour, 0).toISOString();
  const slotEnd = (hour: number): string => new Date(2026, 9, 17, hour + 1, 30).toISOString();

  const buildTable = (hours: number[]): Table => ({
    id: 'table-1',
    tableNumber: 1,
    locationId: 'loc-1',
    capacity: 4,
    timeRanges: hours.map((hour) => ({ start: slotStart(hour), end: slotEnd(hour) })),
  });

  const render = (table: Table): void => {
    fixture.componentRef.setInput('table', table);
    fixture.componentRef.setInput('location', '48 Rustaveli Avenue');
    fixture.componentRef.setInput('date', 'Oct 17, 2026');
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TableCardComponent);
    component = fixture.componentInstance;
  });

  it('renders a fixed grid of half-hour slots from 10:00 to 21:30', () => {
    render(buildTable([11]));

    const labels = Array.from<HTMLElement>(fixture.nativeElement.querySelectorAll('.body-bold')).map(
      (element) => element.textContent?.trim()
    );

    expect(labels.length).toBe(24);
    expect(labels[0]).toBe('10:00');
    expect(labels[1]).toBe('10:30');
    expect(labels[23]).toBe('21:30');
  });

  it('disables half-hours without an available time range', () => {
    render(buildTable([11, 14]));

    const enabled = fixture.nativeElement.querySelectorAll('.time-slot-container:not(.disabled)');
    const disabled = fixture.nativeElement.querySelectorAll('.time-slot-container.disabled');

    expect(enabled.length).toBe(2);
    expect(disabled.length).toBe(22);
  });

  it('emits the time range when an available slot is clicked', () => {
    const table = buildTable([11]);
    render(table);
    const emitted: unknown[] = [];
    component.timeSlotSelected.subscribe((range) => emitted.push(range));

    fixture.nativeElement.querySelectorAll('app-time-slot')[2].click();

    expect(emitted).toEqual([table.timeRanges[0]]);
  });

  describe('with a slot that starts off the half-hour grid', () => {
    const offGridTable = (): Table => ({
      ...buildTable([]),
      timeRanges: [
        {
          start: new Date(2026, 9, 17, 15, 0).toISOString(),
          end: new Date(2026, 9, 17, 16, 30).toISOString(),
        },
        {
          start: new Date(2026, 9, 17, 16, 45).toISOString(),
          end: new Date(2026, 9, 17, 18, 15).toISOString(),
        },
      ],
    });

    const renderedLabels = (): (string | undefined)[] =>
      Array.from<HTMLElement>(fixture.nativeElement.querySelectorAll('.body-bold')).map(
        (element) => element.textContent?.trim()
      );

    it('labels an enabled slot with its real start time, not the half-hour bucket', () => {
      render(offGridTable());

      const labels = renderedLabels();
      const enabled = fixture.nativeElement.querySelectorAll('.time-slot-container:not(.disabled)');

      expect(labels.length).toBe(24);
      expect(labels[10]).toBe('15:00');
      expect(labels[13]).toBe('16:45');
      expect(enabled.length).toBe(2);
    });

    it('keeps the bucket label on disabled buttons', () => {
      render(offGridTable());

      const labels = renderedLabels();

      expect(labels[12]).toBe('16:00');
      expect(labels[14]).toBe('17:00');
    });

    it('emits the off-grid range when its button is clicked', () => {
      const table = offGridTable();
      render(table);
      const emitted: unknown[] = [];
      component.timeSlotSelected.subscribe((range) => emitted.push(range));

      fixture.nativeElement.querySelectorAll('app-time-slot')[13].click();

      expect(emitted).toEqual([table.timeRanges[1]]);
    });
  });

  it('does not emit when a disabled slot is clicked', () => {
    render(buildTable([11]));
    const emitted: unknown[] = [];
    component.timeSlotSelected.subscribe((range) => emitted.push(range));

    fixture.nativeElement.querySelectorAll('app-time-slot')[0].click();

    expect(emitted).toEqual([]);
  });
});

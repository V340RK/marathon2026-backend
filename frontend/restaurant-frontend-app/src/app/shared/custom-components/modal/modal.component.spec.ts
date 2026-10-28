import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ModalComponent } from './modal.component';

@Component({
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [ModalComponent],
  template: `
    <app-modal [title]="'Test title'" [closeOnBackdrop]="closeOnBackdrop" (closed)="closedCount = closedCount + 1">
      <p class="body-text">Body</p>
      <button modal-actions class="action-btn">Action</button>
    </app-modal>
  `,
})
class HostComponent {
  closeOnBackdrop = true;
  closedCount = 0;
}

describe('ModalComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    element = fixture.nativeElement;
  });

  it('renders the title, body and actions', () => {
    expect(element.querySelector('.modal-title')?.textContent).toContain('Test title');
    expect(element.querySelector('.modal-body .body-text')?.textContent).toBe('Body');
    expect(element.querySelector('.modal-actions .action-btn')).not.toBeNull();
  });

  it('emits closed when the close button is clicked', () => {
    element.querySelector<HTMLButtonElement>('.modal-close')!.click();

    expect(host.closedCount).toBe(1);
  });

  it('emits closed when the backdrop is clicked', () => {
    element.querySelector<HTMLElement>('.modal-backdrop')!.click();

    expect(host.closedCount).toBe(1);
  });

  it('does not emit closed when the card is clicked', () => {
    element.querySelector<HTMLElement>('.modal-card')!.click();

    expect(host.closedCount).toBe(0);
  });

  it('does not emit closed on backdrop click when closeOnBackdrop is false', () => {
    host.closeOnBackdrop = false;
    fixture.detectChanges();

    element.querySelector<HTMLElement>('.modal-backdrop')!.click();

    expect(host.closedCount).toBe(0);
  });

  it('emits closed when Escape is pressed', () => {
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(host.closedCount).toBe(1);
  });
});

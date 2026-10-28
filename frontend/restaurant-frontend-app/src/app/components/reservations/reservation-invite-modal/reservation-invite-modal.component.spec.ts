import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { GroupInviteToken } from '@app/models/reservations/guest-invite.model';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ReservationInviteModalComponent } from './reservation-invite-modal.component';

const BASE_RESERVATION: Reservation = {
  id: 'res-1',
  tableId: 'table-1',
  customerId: 'customer-1',
  startTime: new Date(2026, 9, 17, 12, 0),
  endTime: new Date(2026, 9, 17, 14, 0),
  guestCount: 2,
  status: ReservationStatus.Reserved,
  locationAddress: '48 Rustaveli Avenue',
  tableCapacity: 4,
  waiterId: 'waiter-1',
};

describe('ReservationInviteModalComponent', () => {
  let fixture: ComponentFixture<ReservationInviteModalComponent>;
  let component: ReservationInviteModalComponent;
  let element: HTMLElement;
  let toastService: jasmine.SpyObj<ToastService>;
  let router: Router;
  let guestInviteService: jasmine.SpyObj<GuestInviteService>;

  function render(reservation: Reservation): void {
    fixture.componentRef.setInput('reservation', reservation);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);

    guestInviteService = jasmine.createSpyObj<GuestInviteService>('GuestInviteService', [
      'getInviteToken$',
    ]);
    guestInviteService.getInviteToken$.and.returnValue(of({ token: 'abc123xyz' }));

    await TestBed.configureTestingModule({
      imports: [ReservationInviteModalComponent],
      providers: [
        provideRouter([]),
        { provide: ToastService, useValue: toastService },
        { provide: GuestInviteService, useValue: guestInviteService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationInviteModalComponent);
    component = fixture.componentInstance;
    element = fixture.nativeElement;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
  });

  it('renders location, date, time and guests', () => {
    render(BASE_RESERVATION);

    const text = element.textContent ?? '';
    expect(text).toContain('48 Rustaveli Avenue');
    expect(text).toContain('Oct 17, 2026');
    expect(text).toContain('12:00 - 14:00');
    expect(text).toContain('2 people');
  });

  it('uses singular wording for one guest', () => {
    render({ ...BASE_RESERVATION, guestCount: 1 });

    expect(element.textContent).toContain('1 person');
  });

  it('loads the invite token for the reservation and shows the link', () => {
    render(BASE_RESERVATION);

    const input = element.querySelector<HTMLInputElement>('#invite-link-input');
    expect(guestInviteService.getInviteToken$).toHaveBeenCalledOnceWith('res-1');
    expect(input?.value).toBe(`${window.location.origin}/join/abc123xyz`);
    expect(element.querySelector<HTMLButtonElement>('button[aria-label="Copy invite link"]')!.disabled).toBeFalse();
  });

  it('does not request a token or show the invite field for a single guest', () => {
    render({ ...BASE_RESERVATION, guestCount: 1 });

    expect(guestInviteService.getInviteToken$).not.toHaveBeenCalled();
    expect(element.querySelector('#invite-link-input')).toBeNull();
  });

  it('shows a loading placeholder and a disabled copy button while the token loads', () => {
    guestInviteService.getInviteToken$.and.returnValue(new Subject<GroupInviteToken>());
    render({ ...BASE_RESERVATION, guestCount: 3 });

    const input = element.querySelector<HTMLInputElement>('#invite-link-input');
    expect(input!.value).toBe('');
    expect(input!.placeholder).toBe('Generating link...');
    expect(element.querySelector<HTMLButtonElement>('button[aria-label="Copy invite link"]')!.disabled).toBeTrue();
  });

  it('shows an error toast and keeps copy disabled when the token request fails', () => {
    guestInviteService.getInviteToken$.and.returnValue(throwError(() => new Error('boom')));
    render(BASE_RESERVATION);

    const input = element.querySelector<HTMLInputElement>('#invite-link-input');
    expect(toastService.error).toHaveBeenCalled();
    expect(input!.value).toBe('');
    expect(input!.placeholder).toBe('Link is unavailable');
    expect(element.querySelector<HTMLButtonElement>('button[aria-label="Copy invite link"]')!.disabled).toBeTrue();
  });

  it('does not copy or toast when the token has not loaded yet', async () => {
    guestInviteService.getInviteToken$.and.returnValue(new Subject<GroupInviteToken>());
    render(BASE_RESERVATION);
    const writeText = spyOn(navigator.clipboard, 'writeText').and.resolveTo();

    await component.onCopyLink();

    expect(writeText).not.toHaveBeenCalled();
    expect(toastService.success).not.toHaveBeenCalled();
  });

  it('copies the link and shows a success toast', async () => {
    render(BASE_RESERVATION);
    const writeText = spyOn(navigator.clipboard, 'writeText').and.resolveTo();

    await component.onCopyLink();

    expect(writeText).toHaveBeenCalledWith(`${window.location.origin}/join/abc123xyz`);
    expect(toastService.success).toHaveBeenCalledWith('Share it with your friends.', 'Link copied!');
  });

  it('shows an error toast when copying fails', async () => {
    render(BASE_RESERVATION);
    spyOn(navigator.clipboard, 'writeText').and.rejectWith(new Error('denied'));

    await component.onCopyLink();

    expect(toastService.error).toHaveBeenCalled();
    expect(toastService.success).not.toHaveBeenCalled();
  });

  it('closes and navigates to reservations on View Reservation', () => {
    render(BASE_RESERVATION);
    spyOn(component.closed, 'emit');

    element.querySelector<HTMLButtonElement>('.view-reservation-btn')!.click();

    expect(component.closed.emit).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/reservations']);
  });

  it('closes and navigates to the menu on Make Pre-Order', () => {
    render(BASE_RESERVATION);
    spyOn(component.closed, 'emit');

    element.querySelector<HTMLButtonElement>('.pre-order-btn')!.click();

    expect(component.closed.emit).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/menu']);
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GroupInviteToken, GuestInvite } from '@app/models/reservations/guest-invite.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GuestInviteService } from './guest-invite.service';

const BASE_URL = 'https://test.api';

describe('GuestInviteService', () => {
  let service: GuestInviteService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE_URL },
      ],
    });
    service = TestBed.inject(GuestInviteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the invite token for a reservation with GET', () => {
    let result: GroupInviteToken | undefined;

    service.getInviteToken$('res-1').subscribe((response) => (result = response));

    const req = httpMock.expectOne(`${BASE_URL}/reservations/res-1/invite-token`);
    expect(req.request.method).toBe('GET');
    req.flush({ token: 'abc123xyz' });
    expect(result).toEqual({ token: 'abc123xyz' });
  });

  it('encodes the reservation id when requesting the invite token', () => {
    service.getInviteToken$('a/b c').subscribe();

    httpMock.expectOne(`${BASE_URL}/reservations/a%2Fb%20c/invite-token`).flush({ token: 'x' });
  });

  it('propagates HTTP errors from the invite token request', () => {
    let status: number | undefined;

    service.getInviteToken$('res-1').subscribe({ error: (err) => (status = err.status) });

    httpMock
      .expectOne(`${BASE_URL}/reservations/res-1/invite-token`)
      .flush({ error: 'not allowed' }, { status: 400, statusText: 'Bad Request' });
    expect(status).toBe(400);
  });

  it('requests the invite by token with GET and returns the preview', () => {
    const invite: GuestInvite = {
      GroupReservationId: 'group-1',
      LocationId: 'loc-1',
      RestaurantName: 'Green & Tasty',
      LocationAddress: '48 Rustaveli Avenue',
      StartTime: '2026-10-17T12:00:00Z',
      EndTime: '2026-10-17T14:00:00Z',
      MaxGuestCount: 2,
      TableNumber: 3,
      InviterName: 'Kateryna',
    };
    let result: GuestInvite | undefined;

    service.getInvite$('abc123xyz').subscribe((response) => (result = response));

    const req = httpMock.expectOne(`${BASE_URL}/guest/invites/abc123xyz`);
    expect(req.request.method).toBe('GET');
    req.flush(invite);
    expect(result).toEqual(invite);
  });

  it('encodes the token in the URL', () => {
    service.getInvite$('a/b c').subscribe();

    const req = httpMock.expectOne(`${BASE_URL}/guest/invites/a%2Fb%20c`);
    req.flush({});
  });

  it('propagates HTTP errors', () => {
    let status: number | undefined;

    service.getInvite$('expired').subscribe({ error: (err) => (status = err.status) });

    httpMock
      .expectOne(`${BASE_URL}/guest/invites/expired`)
      .flush('gone', { status: 410, statusText: 'Gone' });
    expect(status).toBe(410);
  });
});

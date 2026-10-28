import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Reservation } from '@app/models/reservations/reservation.model';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

const MIN_GUESTS_FOR_INVITE = 2;

@Component({
  selector: 'app-reservation-invite-modal',
  imports: [DatePipe, ModalComponent],
  templateUrl: './reservation-invite-modal.component.html',
  styleUrl: './reservation-invite-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ReservationInviteModalComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly guestInviteService = inject(GuestInviteService);
  private readonly destroyRef = inject(DestroyRef);

  readonly reservation = input.required<Reservation>();

  readonly closed = output<void>();

  readonly showInviteField = computed(() => this.reservation().guestCount >= MIN_GUESTS_FOR_INVITE);

  readonly guestsLabel = computed(() => {
    const count = this.reservation().guestCount;
    return `${count} ${count === 1 ? 'person' : 'people'}`;
  });

  private readonly inviteToken = signal<string | null>(null);
  private readonly tokenLoading = signal(false);
  private readonly tokenFailed = signal(false);

  readonly inviteLink = computed(() => {
    const token = this.inviteToken();
    return token ? `${window.location.origin}/join/${token}` : null;
  });

  readonly invitePlaceholder = computed(() => {
    if (this.tokenFailed()) {
      return 'Link is unavailable';
    }
    return this.tokenLoading() ? 'Generating link...' : 'Link will be available soon';
  });

  ngOnInit(): void {
    if (!this.showInviteField()) {
      return;
    }

    this.tokenLoading.set(true);
    this.guestInviteService
      .getInviteToken$(this.reservation().id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ token }) => {
          this.inviteToken.set(token);
          this.tokenLoading.set(false);
        },
        error: () => {
          this.tokenLoading.set(false);
          this.tokenFailed.set(true);
          this.toastService.error('Could not load the invite link. Please try again later.');
        },
      });
  }

  onClose(): void {
    this.closed.emit();
  }

  async onCopyLink(): Promise<void> {
    const link = this.inviteLink();
    if (!link) {
      return;
    }

    try {
      await navigator.clipboard.writeText(link);
      this.toastService.success('Share it with your friends.', 'Link copied!');
    } catch {
      this.toastService.error('Could not copy the link. Please copy it manually.');
    }
  }

  onViewReservation(): void {
    this.closed.emit();
    this.router.navigate(['/reservations']);
  }

  onMakePreOrder(): void {
    this.closed.emit();
    this.router.navigate(['/menu']);
  }
}

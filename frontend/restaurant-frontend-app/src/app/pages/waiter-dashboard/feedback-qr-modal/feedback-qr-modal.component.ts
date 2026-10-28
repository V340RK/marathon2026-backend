import {
  Component,
  ElementRef,
  Input,
  Output,
  EventEmitter,
  OnInit,
  ViewChild,
  OnDestroy,
  ChangeDetectionStrategy,
  inject
} from '@angular/core';
import QRCodeStyling from 'qr-code-styling';
import { FeedbackQrService } from '@app/services/reservation/feedback-qr.service';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';

@Component({
  selector: 'app-feedback-qr',
  imports: [CustomButtonComponent],
  standalone: true,
  templateUrl: './feedback-qr-modal.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './feedback-qr-modal.component.scss',
})
export class FeedbackQrModalComponent implements OnInit, OnDestroy {
  @Input() locationId!: string;
  @Input() reservationId!: string;
  @Input() waiterName!: string;
  @Input() isNewFeedback = true;

  @ViewChild('qrcodeCanvas', { static: true }) qrCodeElement!: ElementRef;

  @Output() closed = new EventEmitter<void>();

  private qrCode!: QRCodeStyling;
  feedbackUrl!: string;

  private readonly qrService = inject(FeedbackQrService);

  ngOnInit(): void {
    this.qrService.getAnonymousFeedbackToken(this.reservationId).subscribe({
      next: (token) => {
        this.feedbackUrl = this.qrService.generateFeedbackUrl(
          token,
          this.reservationId,
          this.waiterName,
          this.isNewFeedback
        );
        console.log('Feedback link:', this.feedbackUrl);
        this.qrCode = new QRCodeStyling({
          width: Math.min(window.innerWidth * 0.8, 300),
          height: Math.min(window.innerWidth * 0.8, 300),
          data: this.feedbackUrl,
          dotsOptions: {
            color: '#000',
            type: 'rounded',
          },
          backgroundOptions: {
            color: '#ffffff',
          },
        });
        this.qrCode.append(this.qrCodeElement.nativeElement);
      },
      error: (error) => {
        console.error('Error fetching feedback token:', error);
      },
    });
  }

  ngOnDestroy(): void {
    this.qrCode?.update({ data: '' });
  }

  close() {
    this.closed.emit();
  }
}

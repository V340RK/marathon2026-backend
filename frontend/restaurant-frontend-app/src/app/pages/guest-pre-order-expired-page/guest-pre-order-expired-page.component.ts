import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    standalone: true,
    selector: 'app-guest-pre-order-expired-page',
    templateUrl: './guest-pre-order-expired-page.component.html',
    styleUrl: './guest-pre-order-expired-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuestPreOrderExpiredPageComponent {}
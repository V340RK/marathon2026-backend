import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { GuestCartItem } from '@app/pages/guest-pre-order-page/state/guest-preorder.store'; 

@Component({
    standalone: true,
    selector: 'app-guest-cart-drawer',
    templateUrl: './guest-cart-drawer.component.html',
    styleUrl: './guest-cart-drawer.component.scss',
    imports: [CurrencyPipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuestCartDrawerComponent {
    readonly open = input(false);
    readonly items = input<GuestCartItem[]>([]);
    readonly total = input(0);
    readonly busy = input(false);
    readonly error = input<string | null>(null);

    readonly closed = output<void>();
    readonly goToMenu = output<void>();
    readonly inc = output<string>();
    readonly dec = output<string>();
    readonly remove = output<string>();
    readonly submitted = output<void>();
}
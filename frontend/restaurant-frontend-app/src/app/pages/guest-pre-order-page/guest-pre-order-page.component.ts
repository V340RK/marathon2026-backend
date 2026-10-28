import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { GuestMenuDish } from '@app/services/guest-sessions/guest-sessions.service';
import { DishCardComponent } from '@app/shared/custom-components/dish-card/dish-card.component';
import { MainCoverImageComponent } from '@app/shared/custom-components/main-cover-image/main-cover-image.component';
import { ToastService } from '@app/services/toast/toast.service';
import { GuestPreorderStore } from './state/guest-preorder.store';

import { GuestNameModalComponent } from './ui/guest-name-modal/guest-name-modal.component';
import { GuestCartDrawerComponent } from './ui/guest-cart-drawer/guest-cart-drawer.component';
import { GuestPreorderSubmittedComponent } from './ui/guest-preorder-submitted/guest-preorder-submitted.component';

@Component({
    standalone: true,
    selector: 'app-guest-pre-order-page',
    templateUrl: './guest-pre-order-page.component.html',
    styleUrl: './guest-pre-order-page.component.scss',
    imports: [
        RouterLink,
        DishCardComponent,
        MainCoverImageComponent,
        GuestNameModalComponent,
        GuestCartDrawerComponent,
        GuestPreorderSubmittedComponent
    ],
})
export class GuestPreOrderPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly toast = inject(ToastService);

    readonly store = inject(GuestPreorderStore);
    readonly isCartOpen = signal(false);
    readonly selectedCategory = signal<string | null>(null);
    readonly selectedSort = signal<'recommended' | 'price-asc' | 'price-desc'>('recommended');

    readonly categoryOptions = computed(() =>
        this.store.menuCategories().map(key => ({ key, label: this.categoryLabel(key) }))
    );

    readonly visibleDishes = computed(() => {
        const selectedCategory = this.selectedCategory();
        const dishes = Object.entries(this.store.menuByCategory())
            .filter(([category]) => !selectedCategory || category === selectedCategory)
            .flatMap(([category, categoryDishes]) =>
                categoryDishes.map(dish => this.toCardDish(dish, category))
            );

        switch (this.selectedSort()) {
            case 'price-asc':
                return dishes.sort((a, b) => a.price - b.price);
            case 'price-desc':
                return dishes.sort((a, b) => b.price - a.price);
            default:
                return dishes;
        }
    });

    // token from /guest-pre-order/:token
    readonly token = toSignal(
        this.route.paramMap.pipe(map(pm => pm.get('token') ?? '')),
        { initialValue: '' }
    );

    constructor() {
        // init store by token
        effect(() => {
            const t = this.token();
            if (t) this.store.initContext(t);
        });

        // Redirect only for an invite that the backend confirms is invalid or expired.
        effect(() => {
            if (this.store.inviteError() === 'expired') {
                this.router.navigateByUrl('/guest-pre-order-expired');
            }
        });
    }

    openCart() { this.isCartOpen.set(true); }
    closeCart() { this.isCartOpen.set(false); }

    retryInviteLoad(): void {
        const token = this.token();
        if (token) void this.store.loadInviteByToken(token);
    }

    selectCategory(category: string | null): void {
        this.selectedCategory.set(category);
    }

    onSortChanged(event: Event): void {
        this.selectedSort.set((event.target as HTMLSelectElement).value as 'recommended' | 'price-asc' | 'price-desc');
    }

    async addDishToCart(dish: DishListResponse): Promise<void> {
        const added = await this.store.addDish({
            dishId: dish.id,
            title: dish.name,
            price: dish.price,
            imageUrl: dish.imageUrl,
        });

        if (added) {
            this.toast.success(`${dish.name} added to your pre-order.`, 'Added');
        }
    }

    private categoryLabel(category: string): string {
        const labels: Record<string, string> = {
            Appetizer: 'Appetizers',
            MainCourse: 'Main Courses',
            Dessert: 'Desserts',
        };

        return labels[category] ?? category.replace(/([a-z])([A-Z])/g, '$1 $2');
    }

    private toCardDish(dish: GuestMenuDish, category: string): DishListResponse {
        return {
            id: dish.id,
            name: dish.name,
            imageUrl: dish.imageUrl || 'assets/img/dish.png',
            price: dish.price,
            state: dish.selectable ? dish.state : 'OutOfStock',
            weight: dish.weight ?? '',
            dishType: category,
            description: dish.description ?? '',
            calories: dish.calories ?? '',
            proteins: dish.proteins ?? '',
            carbohydrates: dish.carbohydrates ?? '',
            fats: dish.fats ?? '',
            vitamins: dish.vitamins ?? '',
        };
    }

    noop(): void {
        // Guest name modal is mandatory; ignore close attempts (Esc/X)
    }
}
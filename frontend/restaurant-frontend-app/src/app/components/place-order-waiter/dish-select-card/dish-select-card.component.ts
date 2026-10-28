import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dish-select-card',
  imports: [CommonModule],
  templateUrl: './dish-select-card.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './dish-select-card.component.scss',
})
export class DishSelectCardComponent {
  @Input() dish!: DishListResponse;
  @Output() quantityChanged = new EventEmitter<number>();
  @Output() remove = new EventEmitter<void>();

  @Input() quantity = 1; // Replace with dish quantity when implemented

  onIncrement() {
    this.quantity++;
    this.quantityChanged.emit(this.quantity);
  }

  onDecrement() {
    this.quantity = Math.max(1, this.quantity - 1);
    this.quantityChanged.emit(this.quantity);
  }

  canGoDown() {
    return this.quantity > 1;
  }

  onRemove() {
    this.remove.emit();
  }
}

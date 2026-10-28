import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishSelectCardComponent } from './dish-select-card.component';

describe('DishSelectCardComponent', () => {
  let component: DishSelectCardComponent;
  let fixture: ComponentFixture<DishSelectCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishSelectCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishSelectCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

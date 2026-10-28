import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomNumberComponent } from './custom-number.component';

describe('GuestsFilterComponent', () => {
  let component: CustomNumberComponent;
  let fixture: ComponentFixture<CustomNumberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomNumberComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomNumberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

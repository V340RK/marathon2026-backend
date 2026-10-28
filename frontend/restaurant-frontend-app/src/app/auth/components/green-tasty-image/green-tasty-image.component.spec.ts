import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GreenTastyImageComponent } from './green-tasty-image.component';

describe('GreenTastyImageComponent', () => {
  let component: GreenTastyImageComponent;
  let fixture: ComponentFixture<GreenTastyImageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GreenTastyImageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GreenTastyImageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

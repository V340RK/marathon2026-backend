import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MainCoverImageComponent } from './main-cover-image.component';

describe('MainCoverImageComponent', () => {
  let component: MainCoverImageComponent;
  let fixture: ComponentFixture<MainCoverImageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MainCoverImageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MainCoverImageComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput(
      'src',
      'assets/img/main-page-cover.jpg',
    );

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

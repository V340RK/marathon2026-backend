import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeedbackQrModalComponent } from './feedback-qr-modal.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('FeedbackQrModalComponent', () => {
  let component: FeedbackQrModalComponent;
  let fixture: ComponentFixture<FeedbackQrModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [FeedbackQrModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FeedbackQrModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

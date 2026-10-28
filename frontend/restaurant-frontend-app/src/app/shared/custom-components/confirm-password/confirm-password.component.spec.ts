import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';

import { ConfirmPasswordComponent } from './confirm-password.component';

describe('ConfirmPasswordComponent', () => {
  let component: ConfirmPasswordComponent;
  let fixture: ComponentFixture<ConfirmPasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmPasswordComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmPasswordComponent);

    fixture.componentRef.setInput('control', new FormControl(''));
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { By } from '@angular/platform-browser';

import { PasswordComponent } from './password.component';

describe('PasswordComponent', () => {
  let component: PasswordComponent;
  let fixture: ComponentFixture<PasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PasswordComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PasswordComponent);

    // IMPORTANT: required input
    fixture.componentRef.setInput('control', new FormControl(''));
    fixture.detectChanges(); 
    
    component = fixture.componentInstance;
  });
 
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render password input by default', () => {
    const input: HTMLInputElement = fixture.debugElement.query(By.css('input')).nativeElement;
    expect(input.type).toBe('password');
  });

  it('should toggle input type when clicking eye button', () => {
    const inputDe = fixture.debugElement.query(By.css('input'));
    const buttonDe = fixture.debugElement.query(By.css('button.toggle-icon'));

    const input: HTMLInputElement = inputDe.nativeElement;

    expect(input.type).toBe('password');

    buttonDe.triggerEventHandler('click');
    fixture.detectChanges();
    expect(input.type).toBe('text');

    buttonDe.triggerEventHandler('click');
    fixture.detectChanges();
    expect(input.type).toBe('password');
  });

  it('should set focused class on label when input focused/blurred (when label exists)', () => {
    fixture.componentRef.setInput('label', 'Password');
    fixture.detectChanges();

    const inputDe = fixture.debugElement.query(By.css('input'));
    
    inputDe.triggerEventHandler('focus');
    fixture.detectChanges();

    let labelEl: HTMLLabelElement = fixture.debugElement.query(By.css('label.password-label')).nativeElement;
    expect(labelEl.classList.contains('focused')).toBe(true);

    inputDe.triggerEventHandler('blur');
    fixture.detectChanges();

    labelEl = fixture.debugElement.query(By.css('label.password-label')).nativeElement;
    expect(labelEl.classList.contains('focused')).toBe(false);
  });

  it('should not render label when label is empty', ()  => {
    fixture.componentRef.setInput('label', '');
    fixture.detectChanges();

    const label = fixture.debugElement.query(By.css('label.password-label'));
    expect(label).toBeNull();
  });
});

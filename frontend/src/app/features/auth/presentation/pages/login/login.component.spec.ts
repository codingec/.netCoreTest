import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginComponent } from './login.component';
import { LoginUseCase } from '../../../domain/use-cases/login.usecase';
import { of } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        {
          provide: LoginUseCase,
          useValue: {
            execute: jasmine.createSpy().and.returnValue(of(true)),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows a branded card', () => {
    const card: HTMLElement | null = fixture.nativeElement.querySelector('mat-card');
    expect(card).not.toBeNull();
    expect(card!.textContent).toContain('Booking');
  });

  it('uses a submit button that is disabled while the form is invalid', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('type')).toBe('submit');
    expect(button.disabled).toBeTrue();
  });

  it('submits once when the form is submitted (Enter key)', () => {
    const submitSpy = spyOn(component, 'onSubmit');
    component.loginForm.setValue({ username: 'user', password: 'secret' });
    fixture.detectChanges();
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    expect(submitSpy).toHaveBeenCalledTimes(1);
  });
});

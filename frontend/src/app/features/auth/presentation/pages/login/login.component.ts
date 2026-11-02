import { Component, inject, signal } from '@angular/core';

import { ErrorStateMatcher } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { LoginUseCase } from '../../../domain/use-cases/login.usecase';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import {
  FormControl,
  FormGroup,
  FormGroupDirective,
  NgForm,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AuthStore } from '../../../../../core/state/auth.store';

export class MyErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(
    control: FormControl | null,
    form: FormGroupDirective | NgForm | null
  ): boolean {
    const isSubmitted = form && form.submitted;
    return !!(
      control &&
      control.invalid &&
      (control.dirty || control.touched || isSubmitted)
    );
  }
}

@Component({
  selector: 'app-login',
  imports: [MatInputModule, ReactiveFormsModule, MatButtonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  loginForm = new FormGroup({
    username: new FormControl<string | null>(null, [Validators.required]),
    password: new FormControl<string | null>(null, [Validators.required]),
  });
  matcher = new MyErrorStateMatcher();

  readonly loginUseCase = inject(LoginUseCase);

  isLoading = signal(false);
  private router = inject(Router);
  private authStore = inject(AuthStore);

  async onSubmit() {
    if (this.loginForm.valid) {
      const username = this.loginForm.get('username')?.value;
      const password = this.loginForm.get('password')?.value;
      try {
        this.isLoading.set(true);
        const user = await firstValueFrom(
          this.loginUseCase.execute({
            username: username!,
            password: password!,
          })
        );
        console.log('Login successful:', user);
        this.loginForm.reset();
        this.authStore.setSession(user);
        this.router.navigate(['/booking']);
      } catch (error) {
        console.error('Login failed:', error);
      } finally {
        this.isLoading.set(false);
      }
    }
  }
}

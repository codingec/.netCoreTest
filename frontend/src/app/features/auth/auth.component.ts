import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthRepository } from './domain/repositories/auth.repository';
import { AuthImplRepository } from './data/repositories/auth-impl.repository';
import { LoginUseCase } from './domain/use-cases/login.usecase';

@Component({
  selector: 'app-auth',
  imports: [RouterOutlet],
  providers: [
      {
        provide: AuthRepository,
        useClass: AuthImplRepository
      },
      LoginUseCase
    ],
  // standalone: true,
  template: `<router-outlet></router-outlet>`
})

export class AuthComponent{
}

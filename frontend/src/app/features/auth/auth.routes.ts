import { Routes } from '@angular/router';
import { AuthComponent } from './auth.component';
import { AuthRepository } from './domain/repositories/auth.repository';
import { AuthImplRepository } from './data/repositories/auth-impl.repository';
import { LoginUseCase } from './domain/use-cases/login.usecase';

export const authRoutes: Routes = [
  {
    path: '',
    component: AuthComponent,
    children: [
      {
        path: 'login',
        loadComponent: () => import('./presentation/pages/login/login.component').then(m => m.LoginComponent),

      }
    ],
    
  }
];

import { Routes } from '@angular/router';
import { authRoutes } from './features/auth/auth.routes';
import { AuthGuard } from './core/guards/auth.guard';
import { bookingRoutes } from './features/booking/booking.routes';

export const routes: Routes = [
  {
    path: 'auth',
    children: authRoutes
  },
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full'
  },
  {
    path: 'booking',
    children: bookingRoutes
  }
  // {
  //   path: '',
  //   canActivate: [AuthGuard],
  // }
];

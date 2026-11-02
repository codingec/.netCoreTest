import { Routes } from '@angular/router';
import { BookingComponent } from './booking.component';

export const bookingRoutes: Routes = [
  {
    path: '',
    component: BookingComponent,
    children: [
      {
        path: 'working-place',
        loadComponent: () =>
          import(
            './presentation/pages/user/working-place/working-place.component'
          ).then((m) => m.WorkingPlaceComponent),
      },
      {
        path: 'working-place/:id/services',
        loadComponent: () =>
          import('./presentation/pages/user/service/service.component').then(
            (m) => m.ServiceComponent
          ),
      },
      {
        path: 'working-place/:id/services/:sId/service-detail',
        loadComponent: () =>
          import(
            './presentation/pages/user/service-detail/service-detail.component'
          ).then((m) => m.ServiceDetailComponent),
      },
      { path: '', redirectTo: 'working-place', pathMatch: 'full' },
    ],
  },
];

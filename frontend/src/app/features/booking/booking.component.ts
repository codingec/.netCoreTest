import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterOutlet,
} from '@angular/router';
import { OfficeRepository } from './domain/repositories/office.repository';
import { OfficeImplRepository } from './data/repositories/office-impl.repository';
import { GetServicesUseCase } from './domain/use-cases/get-services.usecase';
import { GetWorkingPlacesUseCase } from './domain/use-cases/get-working-places.usecase';
import { ServiceRepository } from './domain/repositories/service.repository';
import { ServiceImplRepository } from './data/repositories/service-impl.repository';
import { BookingStore } from './presentation/state/booking.store';
import { filter, Subscription, switchMap } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SaveBookingUseCase } from './domain/use-cases/save-booking.usecase';
import { BookingRepository } from './domain/repositories/booking.repository';
import { BookingImplRepository } from './data/repositories/booking-impl.repository';
import { GetBookingsByServiceUseCase } from './domain/use-cases/get-bookings-by-service.usecase';

@Component({
  selector: 'app-booking',
  imports: [RouterOutlet],
  providers: [
    {
      provide: OfficeRepository,
      useClass: OfficeImplRepository,
    },
    GetWorkingPlacesUseCase,
    {
      provide: ServiceRepository,
      useClass: ServiceImplRepository,
    },
    GetServicesUseCase,
    {
      provide: BookingRepository,
      useClass: BookingImplRepository,
    },
    SaveBookingUseCase,
    GetBookingsByServiceUseCase,
    BookingStore,
  ],
  template: `<router-outlet></router-outlet>`,
})
export class BookingComponent implements OnInit, OnDestroy {
  bookingStore = inject(BookingStore);
  private suscriptions: Subscription[] = [];
  private _snackbar = inject(MatSnackBar);
  route = inject(ActivatedRoute);
  router = inject(Router);
  ngOnInit(): void {
    this.bookingStore.onLoad();
    const wkplaceSub = this.bookingStore.workingPlaces$
      .pipe(
        filter((list) => list.length > 0),
        switchMap(() => this.route.paramMap)
      )
      .subscribe((params) => {
        console.log('BookingComponent 2 - Retrieved route params:', params);
        const id = params.get('id');
        if (id) {
          try {
            this.bookingStore.setSelectedOfficeId(Number(id));
          } catch (err: any) {
            this._snackbar.open(err.message, '', { duration: 3000 });
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });
    this.suscriptions.push(wkplaceSub);
    const servSub = this.bookingStore.services$
      .pipe(
        filter((s) => s.length > 0),
        switchMap(() => this.route.paramMap)
      )
      .subscribe((params) => {
        const sId = params.get('sId');
        console.log(
          'ServiceDetailComponent - Retrieved sId from route params:',
          sId
        );
        if (sId) {
          try {
            this.bookingStore.setServiceId(Number(sId));
          } catch (err: any) {
            this._snackbar.open(err.message, '', { duration: 3000 });
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });
    this.suscriptions.push(servSub);
  }
  ngOnDestroy(): void {
    if (this.suscriptions.length > 0) {
      this.suscriptions.forEach((s) => s.unsubscribe());
    }
  }
}

import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { BookingStore } from '../../../state/booking.store';
import { filter, Subscription, switchMap } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { toObservable } from '@angular/core/rxjs-interop';
import { AsyncPipe } from '@angular/common';
import { ServiceCardComponent } from '../../../components/service-card/service-card.component';

@Component({
  selector: 'app-service',
  imports: [AsyncPipe, ServiceCardComponent],
  templateUrl: './service.component.html',
  styleUrl: './service.component.scss',
})
export class ServiceComponent implements OnInit, OnDestroy {
  bookingStore = inject(BookingStore);
  route = inject(ActivatedRoute);
  router = inject(Router);
  private _snackbar = inject(MatSnackBar);
  private suscriptions: Subscription[] = [];
  services$ = this.bookingStore.services$;
  ngOnInit(): void {}

  onSelectCard(serviceId: number) {
    this.bookingStore.setServiceId(serviceId);
    const currWId = this.bookingStore.selectedOfficeId()?.id;
    this.router.navigate([
      'booking',
      'working-place',
      currWId,
      'services',
      serviceId,
      'service-detail',
    ]);
  }

  ngOnDestroy(): void {
    if (this.suscriptions.length > 0) {
      this.suscriptions.forEach((s) => s.unsubscribe());
    }
  }
}

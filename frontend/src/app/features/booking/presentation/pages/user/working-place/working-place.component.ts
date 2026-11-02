import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { WorkingPlaceCardComponent } from '../../../components/working-place-card/working-place-card.component';
import { BookingStore } from '../../../state/booking.store';

@Component({
  selector: 'app-working-place',
  imports: [AsyncPipe, WorkingPlaceCardComponent],
  templateUrl: './working-place.component.html',
  styleUrl: './working-place.component.scss',
})
export class WorkingPlaceComponent {
  bookingStore = inject(BookingStore);
  router = inject(Router);
  workingPlaces = this.bookingStore.workingPlaces$;

  onSelectCard(officeId: number) {
    this.router.navigate(['booking', 'working-place', officeId, 'services']);
  }
}

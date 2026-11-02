import { inject, Injectable } from '@angular/core';
import Booking from '../models/booking.model';
import { BookingRepository } from '../repositories/booking.repository';

@Injectable()
export class SaveBookingUseCase {
  private bookingRepository = inject(BookingRepository);
  execute(booking: Booking) {
    return this.bookingRepository.saveBooking(booking);
  }
}

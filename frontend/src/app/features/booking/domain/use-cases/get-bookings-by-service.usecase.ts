import { inject, Injectable } from '@angular/core';
import { ServiceRepository } from '../repositories/service.repository';
import { BookingRepository } from '../repositories/booking.repository';

@Injectable()
export class GetBookingsByServiceUseCase {
  private bookingRepository = inject(BookingRepository);
  execute(serviceId: number) {
    return this.bookingRepository.getBookingsByService(serviceId);
  }
}

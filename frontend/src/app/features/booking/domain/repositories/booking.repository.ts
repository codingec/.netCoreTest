import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import Booking from '../models/booking.model';
import { BookingRow } from '../models/booking-row.model';

@Injectable({ providedIn: 'root' })
export abstract class BookingRepository {
  abstract saveBooking(booking: Booking): Observable<Booking>;
  abstract getBookings(): Observable<Booking[]>;
  abstract getBooking(id: number): Observable<Booking | null>;
  abstract getBookingsByService(serviceId: number): Observable<BookingRow[]>;
}

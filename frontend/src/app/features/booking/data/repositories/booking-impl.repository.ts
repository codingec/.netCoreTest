import { environment } from '../../../../../environments/environment';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { ResponseModel } from '../../../auth/domain/models/response.model';
import Booking from '../../domain/models/booking.model';
import { BookingRepository } from '../../domain/repositories/booking.repository';
import { BookingRow } from '../../domain/models/booking-row.model';

@Injectable({ providedIn: 'root' })
export class BookingImplRepository extends BookingRepository {
  http: HttpClient = inject(HttpClient);
  baseUrl: string = environment.apiUrl;

  override saveBooking(booking: Booking): Observable<Booking> {
    const payload = {
      ...booking,
      startDatetime: this.toLocalIsoNoZ(booking.startDatetime as Date),
      endDatetime: this.toLocalIsoNoZ(booking.endDatetime as Date),
    };
    return this.http
      .post<ResponseModel>(`${this.baseUrl}/Booking`, payload)
      // The API answers 201 with { data: { id } } only.
      .pipe(map((response: ResponseModel) => ({ ...booking, id: response.data?.id })));
  }

  override getBookings(): Observable<Booking[]> {
    return this.http
      .get<ResponseModel>(`${this.baseUrl}/Booking`)
      .pipe(map((response: ResponseModel) => response.data as Booking[]));
  }

  override getBooking(id: number): Observable<Booking | null> {
    return this.http
      .get<ResponseModel>(`${this.baseUrl}/Booking/${id}`)
      .pipe(
        map((response: ResponseModel) => response.data as Booking),
        catchError((err: HttpErrorResponse) =>
          err.status === 404 ? of(null) : throwError(() => err)
        )
      );
  }

  override getBookingsByService(serviceId: number): Observable<BookingRow[]> {
    return this.http
      .get<ResponseModel>(
        `${this.baseUrl}/Booking/Service?serviceId=${serviceId}`
      )
      .pipe(
        map((response: ResponseModel) => response.data as BookingRow[]),
        // The API answers 404 "Bookings not found" when a service has no bookings yet.
        catchError((err: HttpErrorResponse) =>
          err.status === 404 ? of([]) : throwError(() => err)
        )
      );
  }

  private toLocalIsoNoZ(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return (
      `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}:00`
    );
  }
}

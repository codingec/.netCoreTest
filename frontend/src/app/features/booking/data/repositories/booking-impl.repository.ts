import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ResponseModel } from '../../../auth/domain/models/response.model';
import Booking from '../../domain/models/booking.model';
import { BookingRepository } from '../../domain/repositories/booking.repository';
import { OfficeRepositoryMapper } from '../mappers/office.repository.mapper';
import { BookingRow } from '../../domain/models/booking-row.model';

@Injectable({ providedIn: 'root' })
export class BookingImplRepository extends BookingRepository {
  http: HttpClient = inject(HttpClient);
  mapper = new OfficeRepositoryMapper();

  baseUrl: string = 'http://localhost:5170/api';
  relativeUrl: string = '/Office';

  override saveBooking(booking: Booking): Observable<Booking> {
    const payload = {
      ...booking,
      startDatetime: this.toLocalIsoNoZ(booking.startDatetime as Date),
      endDatetime: this.toLocalIsoNoZ(booking.endDatetime as Date),
    };
    return this.http
      .post<ResponseModel>(`${this.baseUrl}/Booking`, payload)
      .pipe(map((response: ResponseModel) => response.data as Booking));
  }

  override getBookings(): Observable<Booking[]> {
    return this.http
      .get<ResponseModel>(`${this.baseUrl}/Booking`)
      .pipe(map((response: ResponseModel) => response.data as Booking[]));
  }

  override getBooking(id: number): Observable<Booking | null> {
    return this.http
      .get<ResponseModel>(`${this.baseUrl}/Booking?id=${id}`)
      .pipe(map((response: ResponseModel) => response.data as Booking | null));
  }

  override getBookingsByService(serviceId: number): Observable<BookingRow[]> {
    return this.http
      .get<ResponseModel>(
        `${this.baseUrl}/Booking/Service?serviceId=${serviceId}`
      )
      .pipe(map((response: ResponseModel) => response.data as BookingRow[]));
  }

  private toLocalIsoNoZ(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return (
      `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}:00`
    );
  }
}

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BookingImplRepository } from './booking-impl.repository';
import { environment } from '../../../../../environments/environment';

describe('BookingImplRepository', () => {
  let repo: BookingImplRepository;
  let http: HttpTestingController;
  const api = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    repo = TestBed.inject(BookingImplRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets one booking from /Booking/{id}', () => {
    let result: unknown;
    repo.getBooking(7).subscribe((b) => (result = b));
    const req = http.expectOne(`${api}/Booking/7`);
    expect(req.request.method).toBe('GET');
    req.flush({ code: 'OK', data: { id: 7 } });
    expect(result).toEqual({ id: 7 });
  });

  it('returns null when the booking does not exist', () => {
    let result: unknown = 'unset';
    repo.getBooking(99).subscribe((b) => (result = b));
    http.expectOne(`${api}/Booking/99`).flush({}, { status: 404, statusText: 'Not Found' });
    expect(result).toBeNull();
  });

  it('treats 404 "Bookings not found" as an empty list', () => {
    let result: unknown;
    repo.getBookingsByService(3).subscribe((rows) => (result = rows));
    http
      .expectOne(`${api}/Booking/Service?serviceId=3`)
      .flush({ code: 'NOT_FOUND' }, { status: 404, statusText: 'Not Found' });
    expect(result).toEqual([]);
  });

  it('still reports other errors for the bookings list', () => {
    let error: any;
    repo.getBookingsByService(3).subscribe({ error: (e) => (error = e) });
    http
      .expectOne(`${api}/Booking/Service?serviceId=3`)
      .flush({}, { status: 500, statusText: 'Server Error' });
    expect(error.status).toBe(500);
  });

  it('posts a booking and returns it with the new id', () => {
    const start = new Date(2026, 10, 4, 9, 30);
    const end = new Date(2026, 10, 4, 10, 0);
    let saved: any;
    repo
      .saveBooking({ serviceDetailId: 2, employeeId: 5, startDatetime: start, endDatetime: end })
      .subscribe((b) => (saved = b));
    const req = http.expectOne(`${api}/Booking`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.startDatetime).toBe('2026-11-04T09:30:00');
    expect(req.request.body.endDatetime).toBe('2026-11-04T10:00:00');
    req.flush({ code: 'OK', data: { id: 42 } }, { status: 201, statusText: 'Created' });
    expect(saved.id).toBe(42);
    expect(saved.serviceDetailId).toBe(2);
  });
});

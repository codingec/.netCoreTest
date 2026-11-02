import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ServiceDetailComponent } from './service-detail.component';
import { BookingStore } from '../../../state/booking.store';
import { provideRouter } from '@angular/router';
import { GetBookingsByServiceUseCase } from '../../../../domain/use-cases/get-bookings-by-service.usecase';
import { signal } from '@angular/core';

describe('ServiceDetailComponent', () => {
  let component: ServiceDetailComponent;
  let fixture: ComponentFixture<ServiceDetailComponent>;

  const getBookingsByServiceUseCaseMock = {
    execute: jasmine.createSpy().and.returnValue(of([])),
  };
  const bookingStoreMock = {
    selectedServiceId: signal<{ id: number } | null>({ id: 1 }),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceDetailComponent],
      providers: [
        { provide: BookingStore, useValue: bookingStoreMock },
        provideRouter([]),
        {
          provide: GetBookingsByServiceUseCase,
          useValue: getBookingsByServiceUseCaseMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

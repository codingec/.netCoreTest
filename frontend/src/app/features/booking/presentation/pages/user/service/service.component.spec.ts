import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceComponent } from './service.component';
import { BookingStore } from '../../../state/booking.store';
import { provideRouter } from '@angular/router';

describe('ServiceComponent', () => {
  let component: ServiceComponent;
  let fixture: ComponentFixture<ServiceComponent>;
  const bookingStoreMock = {};

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceComponent],
      providers: [
        { provide: BookingStore, useValue: bookingStoreMock },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

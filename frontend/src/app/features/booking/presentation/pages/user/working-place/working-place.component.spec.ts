import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkingPlaceComponent } from './working-place.component';
import { BookingStore } from '../../../state/booking.store';

describe('WorkingPlaceComponent', () => {
  let component: WorkingPlaceComponent;
  let fixture: ComponentFixture<WorkingPlaceComponent>;

  const bookingStoreMock = {};
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkingPlaceComponent],
      providers: [{ provide: BookingStore, useValue: bookingStoreMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkingPlaceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

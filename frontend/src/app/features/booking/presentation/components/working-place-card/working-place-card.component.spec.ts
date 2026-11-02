import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkingPlaceCardComponent } from './working-place-card.component';
import { BookingStore } from '../../state/booking.store';
import Office from '../../../domain/models/office.model';

describe('WorkingPlaceCardComponent', () => {
  let component: WorkingPlaceCardComponent;
  let fixture: ComponentFixture<WorkingPlaceCardComponent>;

  const bookingStoreMock = {};
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkingPlaceCardComponent],
      providers: [{ provide: BookingStore, useValue: bookingStoreMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkingPlaceCardComponent);
    component = fixture.componentInstance;
    const workingPlaceMock: Office = {
      id: 1,
      name: 'Test Office',
    } as Office;
    fixture.componentRef.setInput('workingPlace', workingPlaceMock);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

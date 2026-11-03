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

  describe('keyboard and click activation', () => {
    let card: HTMLElement;
    let emitted: unknown[];

    beforeEach(() => {
      emitted = [];
      component.onSelect.subscribe((v: unknown) => emitted.push(v));
      card = fixture.nativeElement.querySelector('.clickable-card');
    });

    it('is focusable and exposed as a button', () => {
      expect(card.getAttribute('tabindex')).toBe('0');
      expect(card.getAttribute('role')).toBe('button');
    });

    it('emits on click', () => {
      card.click();
      expect(emitted).toEqual([1]);
    });

    it('emits on Enter', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      expect(emitted).toEqual([1]);
    });

    it('emits on Space', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));
      expect(emitted).toEqual([1]);
    });
  });
});

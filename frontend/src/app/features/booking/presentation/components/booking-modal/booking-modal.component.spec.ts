import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BookingModal } from './booking-modal.component';
import { of } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

describe('BookingModalComponent', () => {
  let component: BookingModal;
  let fixture: ComponentFixture<BookingModal>;

  const dialogRefMock = {
    close: jasmine.createSpy('close'),
    afterClosed: jasmine.createSpy('afterClosed').and.returnValue(of(true)),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookingModal],
      providers: [
        { provide: MatDialogRef, useValue: dialogRefMock },
        { provide: MAT_DIALOG_DATA, useValue: {} },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BookingModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  const conflict = 'This time is already booked. Please choose another time.';
  const errorBox = (): HTMLElement | null =>
    fixture.nativeElement.querySelector('.modal-error[role="alert"]');

  it('shows the error message inside the dialog', () => {
    component.setError(conflict);
    fixture.detectChanges();

    expect(errorBox()?.textContent).toContain(conflict);
  });

  it('clears the error when the user changes the time', () => {
    component.setError(conflict);
    fixture.detectChanges();

    component.form.patchValue({ startTime: '11:00' });
    fixture.detectChanges();

    expect(component.errorMsg).toBeNull();
    expect(errorBox()).toBeNull();
  });

  it('clears the error when a new save starts', () => {
    component.setError(conflict);

    component.setLoading(true);

    expect(component.errorMsg).toBeNull();
  });

  it('does not close the dialog when an error is set', () => {
    dialogRefMock.close.calls.reset();

    component.setError(conflict);
    fixture.detectChanges();

    expect(dialogRefMock.close).not.toHaveBeenCalled();
  });
});

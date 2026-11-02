import { Component, inject, Inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  FormGroup,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  MatNativeDateModule,
  provideNativeDateAdapter,
} from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import Booking from '../../../domain/models/booking.model';
import { Subject } from 'rxjs/internal/Subject';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { AuthStore } from '../../../../../core/state/auth.store';

@Component({
  selector: 'app-booking-modal',
  standalone: true,
  templateUrl: './booking-modal.component.html',
  styleUrls: ['./booking-modal.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatDialogModule,
    MatSnackBarModule,
    MatSelectModule,
    MatOptionModule,
    MatProgressSpinner,
  ],
  providers: [provideNativeDateAdapter()],
})
export class BookingModal {
  form: FormGroup;
  minDate = new Date();
  maxDate = new Date();
  loading = false;
  errorMsg: string | null = null;
  readonly saveRequested$ = new Subject<any>();

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<BookingModal>,
    private authStore: AuthStore,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private snackBar: MatSnackBar
  ) {
    this.maxDate.setDate(this.minDate.getDate() + 7 * 7);

    this.form = this.fb.group({
      startDate: ['', Validators.required],
      startTime: ['', Validators.required],
      duration: [15, Validators.required],
      endDate: [{ value: '', disabled: true }],
    });

    this.form.valueChanges.subscribe((values) => {
      if (values.startDate && values.startTime && values.duration) {
        this.updateEndDate(values.startDate, values.startTime, values.duration);
      }
    });
  }

  dateFilter = (date: Date | null): boolean => {
    const day = (date || new Date()).getDay();
    return day !== 0 && day !== 6;
  };

  setLoading(isLoading: boolean): void {
    this.loading = isLoading;
    if (isLoading) this.errorMsg = null;
  }

  setError(message: string): void {
    this.errorMsg = message;
  }

  save(): void {
    if (this.form.valid) {
      const start = this.combineDateTime(
        this.form.value.startDate,
        this.form.value.startTime
      );
      const duration = this.form.value.duration;
      const end = new Date(start);
      end.setMinutes(end.getMinutes() + duration);

      if (start < this.minDate) {
        this.showInvalidBookingMessage(
          'Start date/time cannot be in the past.'
        );
        return;
      }

      if (end <= start) {
        this.showInvalidBookingMessage(
          'End datetime must be after start datetime.'
        );
        return;
      }

      if (!this.isBusinessHours(start) || !this.isBusinessHours(end)) {
        this.showInvalidBookingMessage(
          'Bookings must be between 9:00 AM and 5:00 PM.'
        );
        return;
      }

      if (start > this.maxDate || end > this.maxDate) {
        this.showInvalidBookingMessage(
          'Bookings cannot exceed 7 weeks in the future.'
        );
        return;
      }

      const booking: Booking = {
        serviceDetailId: this.data?.id,
        employeeId: this.authStore.getEmployeeId(),
        startDatetime: start,
        endDatetime: end,
      };

      this.saveRequested$.next(booking);
    }
  }

  close(): void {
    this.dialogRef.close(null);
  }

  private combineDateTime(date: Date, time: string): Date {
    const [hours, minutes] = time.split(':').map(Number);
    const result = new Date(date);
    result.setHours(hours, minutes, 0, 0);
    return result;
  }

  private updateEndDate(date: Date, time: string, duration: number): void {
    if (date && time && duration) {
      const start = this.combineDateTime(date, time);
      const end = new Date(start);
      end.setMinutes(end.getMinutes() + duration);
      this.form.get('endDate')?.setValue(end, { emitEvent: false });
    } else {
      this.form
        .get('endDate')
        ?.reset({ value: '', disabled: true }, { emitEvent: false });
    }
  }

  private isBusinessHours(dateTime: Date): boolean {
    const hour = dateTime.getHours();
    return hour >= 9 && hour < 17;
  }

  showInvalidBookingMessage(message: string) {
    this.snackBar.open(message, 'OK', {
      duration: 4000,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['booking-snackbar'],
    });
  }
}

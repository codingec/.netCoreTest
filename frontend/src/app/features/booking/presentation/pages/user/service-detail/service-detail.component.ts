import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import {
  catchError,
  delay,
  EMPTY,
  filter,
  finalize,
  Subscription,
  switchMap,
  tap,
} from 'rxjs';
import { SaveBookingUseCase } from '../../../../domain/use-cases/save-booking.usecase';
import { BookingModal } from '../../../components/booking-modal/booking-modal.component';
import { ServiceDetailCardComponent } from '../../../components/service-detail-card/service-detail-card.component';
import { BookingStore } from '../../../state/booking.store';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { BookingRow } from '../../../../domain/models/booking-row.model';
import { CommonModule } from '@angular/common';
import { GetBookingsByServiceUseCase } from '../../../../domain/use-cases/get-bookings-by-service.usecase';

@Component({
  selector: 'app-service-detail',
  imports: [
    CommonModule,
    ServiceDetailCardComponent,
    MatSelectModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatTableModule,
  ],
  providers: [provideNativeDateAdapter(), SaveBookingUseCase],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './service-detail.component.html',
  styleUrl: './service-detail.component.scss',
})
export class ServiceDetailComponent implements OnInit, OnDestroy {
  isLoading = signal(false);
  bookingStore = inject(BookingStore);
  route = inject(ActivatedRoute);
  router = inject(Router);
  dialog = inject(MatDialog);
  private _snackbar = inject(MatSnackBar);
  private suscriptions: Subscription[] = [];
  service = this.bookingStore.selectedServiceId;
  private readonly saveBookingUseCase = inject(SaveBookingUseCase);
  private readonly getBookingsByServiceUseCase = inject(
    GetBookingsByServiceUseCase
  );
  displayedColumns: string[] = [
    'employeeName',
    'serviceDetailName',
    'startDateTime',
    'endDateTime',
  ];
  dataSource = new MatTableDataSource<BookingRow>([]);

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings() {
    const serviceModel = this.bookingStore.selectedServiceId;
    var id = serviceModel()?.id;

    if (!id) {
      this.dataSource.data = [];
      return;
    }

    this.isLoading.set(true);
    this.suscriptions.push(
      this.getBookingsByServiceUseCase.execute(id).subscribe({
        next: (bookings) => {
          this.dataSource.data = bookings;
          this.isLoading.set(false);
        },
        error: (err) => {
          this._snackbar.open(this.toUserMessage(err), '', {
            duration: 3000,
          });
          this.isLoading.set(false);
        },
      })
    );
  }

  ngOnDestroy(): void {
    if (this.suscriptions.length > 0) {
      this.suscriptions.forEach((s) => s.unsubscribe());
    }
  }

  openModal(detail: any) {
    const dialogRef = this.dialog.open(BookingModal, {
      width: '400px',
      data: detail,
    });

    const modal = dialogRef.componentInstance;

    modal.saveRequested$
      .pipe(
        tap(() => modal.setLoading(true)),
        delay(5000),
        switchMap((dto) =>
          this.saveBookingUseCase.execute(dto).pipe(
            tap((saved) => dialogRef.close(saved)),
            catchError((err) => {
              modal.setError(this.toUserMessage(err));
              return EMPTY;
            }),
            finalize(() => modal.setLoading(false))
          )
        )
      )
      .subscribe();

    dialogRef
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe((saved) => {
        this._snackbar.open('Booking saved successfully!', '', {
          duration: 3000,
        });
      });
  }

  private toUserMessage(err: any): string {
    return err?.error?.message ?? 'Error. Please try again.';
  }
}

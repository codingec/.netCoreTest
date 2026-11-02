import { Injectable } from '@angular/core';
import Office from '../models/office.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export abstract class OfficeRepository {
  abstract getOficces(): Observable<Office[]>;
}

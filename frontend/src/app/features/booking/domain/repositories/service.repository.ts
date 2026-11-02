import { Injectable } from '@angular/core';
import ServiceModel from '../models/service.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export abstract class ServiceRepository {
  abstract getServices(officeId: number): Observable<ServiceModel[]>;
}

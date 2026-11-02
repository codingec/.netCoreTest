import { inject, Injectable } from '@angular/core';
import { OfficeRepository } from '../../domain/repositories/office.repository';
import { map, Observable, tap } from 'rxjs';
import Office from '../../domain/models/office.model';
import { HttpClient } from '@angular/common/http';
import { AuthModel } from '../../../auth/domain/models/auth.model';
import { ResponseModel } from '../../../auth/domain/models/response.model';
import OfficeEntity from '../entities/office.entity';
import { OfficeRepositoryMapper } from '../mappers/office.repository.mapper';

@Injectable({ providedIn: 'root' })
export class OfficeImplRepository extends OfficeRepository {
  http: HttpClient = inject(HttpClient);
  mapper = new OfficeRepositoryMapper();

  baseUrl: string = 'http://localhost:5170/api';
  relativeUrl: string = '/Office';

  override getOficces(): Observable<Office[]> {
    return this.http
      .get<ResponseModel>(this.baseUrl + this.relativeUrl)
      .pipe(
        map((response: ResponseModel) =>
          response.data.map((entity: OfficeEntity) =>
            this.mapper.mapFrom(entity)
          )
        )
      );
  }
}

import { inject, Injectable } from '@angular/core';
import { ServiceRepository } from '../../domain/repositories/service.repository';
import { map, Observable } from 'rxjs';
import ServiceModel from '../../domain/models/service.model';
import { OfficeRepositoryMapper } from '../mappers/office.repository.mapper';
import { HttpClient } from '@angular/common/http';
import { ResponseModel } from '../../../auth/domain/models/response.model';
import OfficeEntity from '../entities/office.entity';
import { ServiceDetailRepositoryMapper } from '../mappers/service-detail.repository.mapper';
import ServiceDetailEntity from '../entities/service-detail.entity';
import { ServiceRepositoryMapper } from '../mappers/service.repository.mapper';
import ServiceEntity from '../entities/service.entity';

@Injectable({ providedIn: 'root' })
export class ServiceImplRepository extends ServiceRepository {
  http: HttpClient = inject(HttpClient);
  mapper = new ServiceRepositoryMapper();

  baseUrl: string = 'http://localhost:5170/api';
  relativeUrl: string = '/Services';

  override getServices(officeId: number): Observable<ServiceModel[]> {
    return this.http
      .get<ResponseModel>(
        this.baseUrl + this.relativeUrl + `?officeId=${officeId}`
      )
      .pipe(
        map((response: ResponseModel) =>
          response.data.map((entity: ServiceEntity) =>
            this.mapper.mapFrom(entity)
          )
        )
      );
  }
}

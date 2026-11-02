import { Mapper } from '../../../../shared/utils/mapper';
import ServiceDetailModel from '../../domain/models/service-detail.model';
import ServiceDetailEntity from '../entities/service-detail.entity';

export class ServiceDetailRepositoryMapper extends Mapper<
  ServiceDetailEntity,
  ServiceDetailModel
> {
  override mapFrom(param: ServiceDetailEntity): ServiceDetailModel {
    return {
      id: param.id,
      serviceId: param.serviceId,
      name: param.name,
      description: param.description,
      capacity: param.capacity,
      iconUrl: param.icon,
    };
  }
  override mapTo(param: ServiceDetailModel): ServiceDetailEntity {
    return {
      id: param.id,
      serviceId: param.serviceId,
      name: param.name,
      description: param.description,
      capacity: param.capacity,
      icon: param.iconUrl,
    };
  }
}

import { Mapper } from '../../../../shared/utils/mapper';
import ServiceModel from '../../domain/models/service.model';
import ServiceEntity from '../entities/service.entity';
import { ServiceDetailRepositoryMapper } from './service-detail.repository.mapper';

export class ServiceRepositoryMapper extends Mapper<
  ServiceEntity,
  ServiceModel
> {
  serviceDetailMapper = new ServiceDetailRepositoryMapper();

  override mapFrom(param: ServiceEntity): ServiceModel {
    return {
      id: param.id,
      name: param.name,
      description: param.description,
      officeId: param.officeId,
      quantity: param.quantity,
      maxBookingsPerWeek: param.maxBookingsPerWeek,
      bookingPerTime: param.bookingPerTime,
      minMax: param.minMax,
      imageUrl: param.image,
      serviceDetails: param.serviceDetails.map((detail) =>
        this.serviceDetailMapper.mapFrom(detail)
      ),
    };
  }
  override mapTo(param: ServiceModel): ServiceEntity {
    return {
      id: param.id,
      name: param.name,
      description: param.description,
      officeId: param.officeId,
      quantity: param.quantity,
      maxBookingsPerWeek: param.maxBookingsPerWeek,
      bookingPerTime: param.bookingPerTime,
      minMax: param.minMax,
      image: param.imageUrl,
      serviceDetails: param.serviceDetails.map((detail) =>
        this.serviceDetailMapper.mapTo(detail)
      ),
    };
  }
}

import { Mapper } from '../../../../shared/utils/mapper';
import Office from '../../domain/models/office.model';
import OfficeEntity from '../entities/office.entity';

export class OfficeRepositoryMapper extends Mapper<OfficeEntity, Office> {
  override mapFrom(param: OfficeEntity): Office {
    return {
      id: param.id,
      name: param.name,
      imageUrl: param.image,
    };
  }
  override mapTo(param: Office): OfficeEntity {
    return {
      id: param.id,
      name: param.name,
      image: param.imageUrl,
    };
  }
}

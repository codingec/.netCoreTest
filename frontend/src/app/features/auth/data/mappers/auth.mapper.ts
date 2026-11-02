import { Mapper } from '../../../../shared/utils/mapper';

import { AuthModel } from '../../domain/models/auth.model';
import { AuthEntity } from '../entities/auth';
import { DataTokenMapper } from './data-token.mapper';

export class AuthMapper extends Mapper<AuthEntity, AuthModel> {
  dataTokenMapper = new DataTokenMapper();

  override mapFrom(param: AuthEntity): AuthModel {
    return {
      code: param.code,
      data: this.dataTokenMapper.mapFrom(param.data)
    };
  }

  override mapTo(param: AuthModel): AuthEntity {
    return {
      code: param.code,
      data: this.dataTokenMapper.mapTo(param.data)
    };
  }
}

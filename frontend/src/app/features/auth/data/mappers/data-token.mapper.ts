import { Mapper } from '../../../../shared/utils/mapper';

import { DataTokenEntity } from '../entities/data-token.entity';
import { DataTokenModel } from '../../domain/models/data-token.model';

export class DataTokenMapper extends Mapper<DataTokenEntity, DataTokenModel> {
  mapFrom(param: DataTokenEntity): DataTokenModel {
    return {
      token: param.token,
      expiresAt: param.expiresAt,
      userId: param.userId,
      userName: param.userName,
      roles: param.roles,
      permissions: param.permissions,
      employeeId: param.employeeId,
    };
  }

  mapTo(param: DataTokenModel): DataTokenEntity {
    return {
      token: param.token,
      expiresAt: param.expiresAt,
      userId: param.userId,
      userName: param.userName,
      roles: param.roles,
      permissions: param.permissions,
      employeeId: param.employeeId,
    };
  }
}

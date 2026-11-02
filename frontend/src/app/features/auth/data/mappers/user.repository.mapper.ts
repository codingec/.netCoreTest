// infrastructure/mappers/user-implementation.repository.mapper.ts
import { Mapper } from '../../../../shared/utils/mapper';
import UserModel from '../../domain/models/user.model';
import UserEntity from '../entities/user.entity';
import UserRoleModel from '../../domain/models/role.model';
import UserRoleEntity from '../entities/role.entity';

export class UserImplementationRepositoryMapper extends Mapper<UserEntity, UserModel> {
  mapFrom(param: UserEntity): UserModel {
    return {
      id: param.id,
      role: this.mapRoleEntityToModel(param.role)
    };
  }

  mapTo(param: UserModel): UserEntity {
    return {
      id: param.id,
      role: this.mapRoleModelToEntity(param.role)
    };
  }

  private mapRoleEntityToModel(role: UserRoleEntity): UserRoleModel {
    switch (role) {
      case UserRoleEntity.ADMIN:
        return UserRoleModel.ADMIN;
      case UserRoleEntity.USER:
        return UserRoleModel.USER;
      default:
        throw new Error(`Unknown role entity: ${role}`);
    }
  }

  private mapRoleModelToEntity(role: UserRoleModel): UserRoleEntity {
    switch (role) {
      case UserRoleModel.ADMIN:
        return UserRoleEntity.ADMIN;
      case UserRoleModel.USER:
        return UserRoleEntity.USER;
      default:
        throw new Error(`Unknown role model: ${role}`);
    }
  }
}

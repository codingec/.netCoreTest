import UserRoleModel from './role.model';

export default interface UserModel {
  id: string;
  role: UserRoleModel;
  employeeId: number | null;
}

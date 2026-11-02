export interface DataTokenModel {
  token: string;
  expiresAt: string;
  userId: string;
  userName: string;
  roles: string[];
  permissions: string[];
  employeeId?: number;
}

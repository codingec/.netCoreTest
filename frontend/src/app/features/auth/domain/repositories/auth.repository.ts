import { Observable } from 'rxjs';
import UserModel from '../models/user.model';
import { AuthModel } from '../models/auth.model';

export abstract class AuthRepository {
  abstract login(params: { username: string, password: string }): Observable<AuthModel>;
  abstract logout(): Observable<void>;
  abstract getCurrentUser(): Observable<UserModel>;
}

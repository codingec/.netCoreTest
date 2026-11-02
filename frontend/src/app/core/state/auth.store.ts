import { computed, Injectable, signal } from '@angular/core';
import { BehaviorSubject, map, Observable } from 'rxjs';
import User from '../../features/auth/domain/models/user.model';
import UserRole from '../../features/auth/domain/models/role.model';
import { AuthModel } from '../../features/auth/domain/models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _user = new BehaviorSubject<User | null>(null);
  private readonly _role = new BehaviorSubject<UserRole | null>(null);
  private readonly _authData = new BehaviorSubject<AuthModel | null>(null);

  readonly user$ = this._user.asObservable();
  readonly role$ = this._role.asObservable();
  readonly authData$ = this._authData.asObservable();
  readonly isLoggedIn$ = this.authData$.pipe(map(Boolean));
  readonly employeeId$ = this.user$.pipe(map((u) => u?.employeeId ?? null));

  setSession(authData: AuthModel) {
    this._authData.next(authData);
    const role = authData.data.roles.includes('Admin')
      ? UserRole.ADMIN
      : UserRole.USER;
    const user: User = {
      id: authData.data.userId,
      role: role,
      employeeId: authData.data.employeeId ?? null,
    };
    this._user.next(user);
    this._role.next(user.role);
    localStorage.setItem('auth', JSON.stringify(authData));
  }

  loadSession() {
    const storedAuth = localStorage.getItem('auth');
    if (storedAuth) {
      const authData: AuthModel = JSON.parse(storedAuth);
      this.setSession(authData);
    }
  }

  clearSession() {
    this._user.next(null);
    this._role.next(null);
    this._authData.next(null);
    localStorage.removeItem('auth');
  }

  hasRole(role: UserRole): Observable<boolean> {
    return this.role$.pipe(map((r) => r === role));
  }

  getCurrentRole(): UserRole | null {
    return this._role.value;
  }

  getEmployeeId(): number | null {
    return this._user.value?.employeeId ?? null;
  }
}

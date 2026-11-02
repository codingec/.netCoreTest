import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';

import { AuthStore } from '../../../../core/state/auth.store';
import { AuthModel } from '../../domain/models/auth.model';
import UserRoleModel from '../../domain/models/role.model';
import UserModel from '../../domain/models/user.model';
import { AuthRepository } from '../../domain/repositories/auth.repository';

@Injectable({ providedIn: 'root' })
export class AuthImplRepository extends AuthRepository {
  private http = inject(HttpClient);
  private authStore = inject(AuthStore);

  private baseUrl = 'http://localhost:5170/api';
  private relativeUrl = '/Auth/login';

  override login(params: {
    username: string;
    password: string;
  }): Observable<AuthModel> {
    return this.http
      .post<AuthModel>(this.baseUrl + this.relativeUrl, {
        email: params.username,
        password: params.password,
      })
      .pipe(
        tap((authResponse) => {
          this.authStore.setSession(authResponse);
        })
      );
  }

  override logout(): Observable<void> {
    this.authStore.clearSession();
    return of(void 0);
  }

  override getCurrentUser(): Observable<UserModel> {
    return this.authStore.authData$.pipe(
      map((auth) => {
        if (!auth) {
          throw new Error('No Session Found');
        }
        const role = auth.data.roles.includes('Admin')
          ? UserRoleModel.ADMIN
          : UserRoleModel.USER;

        const user: UserModel = {
          id: auth.data.userId,
          role,
          employeeId: auth.data.employeeId ?? null,
        };

        return user;
      })
    );
  }
}

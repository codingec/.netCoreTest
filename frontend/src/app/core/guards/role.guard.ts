import { Injectable } from '@angular/core';
import { AuthStore } from '../state/auth.store';
import { ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot } from '@angular/router';
import UserRoleModel from '../../features/auth/domain/models/role.model';
import { map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {

  constructor(
    private authStore: AuthStore,
    private router: Router
  ) { }
  canActivate(): boolean {
    const role = this.authStore.getCurrentRole();
    if (role === UserRoleModel.ADMIN) return true;

    this.router.navigate(['/not-authorized']);
    return false;
  }

}

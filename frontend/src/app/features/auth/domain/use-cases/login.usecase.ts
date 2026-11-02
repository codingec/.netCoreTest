import { Injectable, inject } from "@angular/core";
import { AuthRepository } from "../repositories/auth.repository";

@Injectable({ providedIn: 'root' })
export class LoginUseCase {
  private authRepository = inject(AuthRepository);

  execute(params: { username: string; password: string }) {
    return this.authRepository.login(params);
  }
}

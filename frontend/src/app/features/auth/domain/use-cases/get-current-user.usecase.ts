import { Injectable, inject } from "@angular/core";
import { AuthRepository } from "../repositories/auth.repository";

@Injectable({ providedIn: 'root' })
export class GetCurrentUserUseCase {
  private authRepository = inject(AuthRepository);

  execute() {
    return this.authRepository.getCurrentUser();
  }
}

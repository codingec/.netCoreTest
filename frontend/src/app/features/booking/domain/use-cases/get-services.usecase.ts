import { inject, Injectable } from '@angular/core';
import { ServiceRepository } from '../repositories/service.repository';

@Injectable()
export class GetServicesUseCase {
  private serviceRepository = inject(ServiceRepository);
  execute(officeId: number) {
    return this.serviceRepository.getServices(officeId);
  }
}

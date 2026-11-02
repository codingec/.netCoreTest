import { inject, Injectable } from '@angular/core';
import { OfficeRepository } from '../repositories/office.repository';

@Injectable()
export class GetWorkingPlacesUseCase {
  private workingPlacesRepository = inject(OfficeRepository);

  execute() {
    return this.workingPlacesRepository.getOficces();
  }
}

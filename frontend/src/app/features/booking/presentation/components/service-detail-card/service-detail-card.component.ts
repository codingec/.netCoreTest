import { Component, input, linkedSignal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import ServiceDetailModel from '../../../domain/models/service-detail.model';

@Component({
  selector: 'app-service-detail-card',
  imports: [MatCardModule,MatListModule],
  templateUrl: './service-detail-card.component.html',
  styleUrl: './service-detail-card.component.scss'
})
export class ServiceDetailCardComponent {
  serviceDetail= input.required<ServiceDetailModel>();
  characteristics = linkedSignal(() => {
    return [
      {
        name: "Capacity",
        value: this.serviceDetail().capacity
      },
    ]
  })
}

import { Component, input, linkedSignal, output } from '@angular/core';
import ServiceModel from '../../../domain/models/service.model';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-service-card',
  imports: [MatCardModule, MatListModule],
  templateUrl: './service-card.component.html',
  styleUrl: './service-card.component.scss'
})
export class ServiceCardComponent {
  service = input.required<ServiceModel>();
  onSelected = output<number>();

  characteristics = linkedSignal(() => {
    return [
      {
        name: "Cantidad",
        value: this.service().quantity
      },
      {
        name: "Max booking per week",
        value: this.service().maxBookingsPerWeek
      }
    ]
  })

}

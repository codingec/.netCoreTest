import { Component, input, output } from '@angular/core';
import Office from '../../../domain/models/office.model';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-working-place-card',
  imports: [MatCardModule],
  templateUrl: './working-place-card.component.html',
  styleUrl: './working-place-card.component.scss',
})
export class WorkingPlaceCardComponent {
  workingPlace = input.required<Office>();
  onSelect = output<number>();
}

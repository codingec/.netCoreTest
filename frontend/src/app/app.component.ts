import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Location } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { filter, map } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule, MatIconModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'Booking';

  private router = inject(Router);
  private location = inject(Location);

  /** URL after the latest completed navigation; empty until the first one finishes. */
  private url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects)
    ),
    { initialValue: '' }
  );

  showToolbar = computed(() => {
    const url = this.url();
    return url !== '' && !url.startsWith('/auth');
  });

  showBack = computed(
    () => this.showToolbar() && !/^\/booking(\/working-place)?\/?$/.test(this.url())
  );

  back() {
    this.location.back();
  }
}

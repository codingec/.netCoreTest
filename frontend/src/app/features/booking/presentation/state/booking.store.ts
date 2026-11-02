import { inject, Injectable, linkedSignal, signal } from '@angular/core';
import Office from '../../domain/models/office.model';
import { BehaviorSubject, combineLatest, filter, firstValueFrom } from 'rxjs';
import ServiceModel from '../../domain/models/service.model';
import { GetWorkingPlacesUseCase } from '../../domain/use-cases/get-working-places.usecase';
import { GetServicesUseCase } from '../../domain/use-cases/get-services.usecase';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';

@Injectable()
export class BookingStore {
  private readonly getWorkingPlacesUseCase = inject(GetWorkingPlacesUseCase);
  private readonly getServicesUseCase = inject(GetServicesUseCase);

  private readonly _workingPlaces = new BehaviorSubject<Office[]>([]);
  readonly workingPlaces$ = this._workingPlaces.asObservable();
  readonly selectedOfficeId = signal<Office | null>(null);

  private readonly _services = new BehaviorSubject<ServiceModel[]>([]);
  readonly services$ = this._services.asObservable();
  readonly selectedServiceId = signal<ServiceModel | null>(null);
  isLoadingServices = signal(false);
  router = inject(Router);
  rootRoute = inject(ActivatedRoute);

  constructor() {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.checkRouteState());
  }
  private checkRouteState() {
    // --- Handle working place logic ---
    combineLatest([this.workingPlaces$, this.rootRoute.paramMap])
      .pipe(filter(([places]) => places.length > 0))
      .subscribe(([places]) => {
        const params = this.getDeepRouteParams();
        const wId = params['id'];
        if (wId) {
          try {
            this.setSelectedOfficeId(Number(wId));
          } catch (err: any) {
            console.warn('Invalid working place ID:', err.message);
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });

    // --- Handle service logic ---
    combineLatest([this.services$, this.rootRoute.paramMap])
      .pipe(filter(([services]) => services.length > 0))
      .subscribe(([services]) => {
        const params = this.getDeepRouteParams();
        const sId = params['sId'];
        if (sId) {
          try {
            this.setServiceId(Number(sId));
          } catch (err: any) {
            console.warn('Invalid service ID:', err.message);
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });
  }
  private getDeepRouteParams(): Record<string, string> {
    let route = this.rootRoute;
    while (route.firstChild) {
      route = route.firstChild;
    }

    const paramMap = route.snapshot.paramMap;
    const params: Record<string, string> = {};
    for (const key of paramMap.keys) {
      params[key] = paramMap.get(key)!;
    }
    return params;
  }

  getServices() {
    return this._services.value;
  }

  setServices(services: ServiceModel[]) {
    this._services.next(services);
  }

  getSelectedWorkingPlace() {
    return this.selectedOfficeId();
  }

  setServiceId(sId: number) {
    const service = this._services.value.find((s) => s.id === sId);
    if (!!service) {
      this.selectedServiceId.set(service);
    } else {
      throw Error('The id cannot be found');
    }
  }

  setSelectedOfficeId(id: number) {
    if (!this.selectedOfficeId() && this.selectedOfficeId()?.id === id) {
      return;
    }
    const place = this._workingPlaces.value.find((place) => place.id === id);
    if (!!place) {
      this.selectedOfficeId.set(place);
      this.loadServices(id);
    } else {
      throw Error('The id cannot be found');
    }
  }

  setWorkingPlaces(places: Office[]) {
    this._workingPlaces.next(places);
  }

  async onLoad() {
    const wPlaces = await firstValueFrom(
      this.getWorkingPlacesUseCase.execute()
    );
    console.log('wPlaces', wPlaces);
    this.setWorkingPlaces(wPlaces);

    //TESTING
    // if (wPlaces.length > 0) {
    //   this.setSelectedWorkingPlaceId(wPlaces[0].id);
    // }
  }

  async loadServices(officeId: number) {
    try {
      this.isLoadingServices.set(true);
      const services = await firstValueFrom(
        this.getServicesUseCase.execute(officeId)
      );
      this.setServices(services);

      // TESTING
      // if (services.length > 0) {
      //   this.setServiceId(services[0].id);
      // }
    } catch {
      this.setServices([]);
    } finally {
      this.isLoadingServices.set(false);
    }
  }
}

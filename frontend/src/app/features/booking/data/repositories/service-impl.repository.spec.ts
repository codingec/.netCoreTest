import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ServiceImplRepository } from './service-impl.repository';
import { environment } from '../../../../../environments/environment';

describe('ServiceImplRepository', () => {
  let repo: ServiceImplRepository;
  let http: HttpTestingController;

  const entity = (id: number, officeId: number) => ({
    id,
    officeId,
    name: `S${id}`,
    description: '',
    quantity: 1,
    maxBookingsPerWeek: 1,
    bookingPerTime: 1,
    minMax: '',
    image: '',
    serviceDetails: [],
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    repo = TestBed.inject(ServiceImplRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('returns only the services of the requested office', () => {
    let ids: number[] = [];
    repo.getServices(1).subscribe((s) => (ids = s.map((x) => x.id)));
    http
      .expectOne(`${environment.apiUrl}/Services?officeId=1`)
      .flush({ code: 'OK', data: [entity(1, 1), entity(2, 2), entity(3, 1)] });
    expect(ids).toEqual([1, 3]);
  });
});

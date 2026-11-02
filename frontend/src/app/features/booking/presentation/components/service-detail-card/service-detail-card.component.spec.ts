import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceDetailCardComponent } from './service-detail-card.component';
import ServiceDetailModel from '../../../domain/models/service-detail.model';

describe('ServiceDetailCardComponent', () => {
  let component: ServiceDetailCardComponent;
  let fixture: ComponentFixture<ServiceDetailCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceDetailCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceDetailCardComponent);
    component = fixture.componentInstance;
    const serviceDetailMock: ServiceDetailModel = {
      id: 1,
      name: 'Test Service',
    } as ServiceDetailModel;
    fixture.componentRef.setInput('serviceDetail', serviceDetailMock);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

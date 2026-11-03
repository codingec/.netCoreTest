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

  describe('keyboard and click activation', () => {
    let card: HTMLElement;
    let emitted: unknown[];

    beforeEach(() => {
      emitted = [];
      component.selected.subscribe((v: unknown) => emitted.push(v));
      card = fixture.nativeElement.querySelector('.clickable-card');
    });

    it('is focusable and exposed as a button', () => {
      expect(card.getAttribute('tabindex')).toBe('0');
      expect(card.getAttribute('role')).toBe('button');
    });

    it('emits on click', () => {
      card.click();
      expect(emitted).toEqual([component.serviceDetail()]);
    });

    it('emits on Enter', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      expect(emitted).toEqual([component.serviceDetail()]);
    });

    it('emits on Space', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));
      expect(emitted).toEqual([component.serviceDetail()]);
    });
  });
});

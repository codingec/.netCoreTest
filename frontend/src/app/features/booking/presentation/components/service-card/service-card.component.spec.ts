import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceCardComponent } from './service-card.component';
import ServiceModel from '../../../domain/models/service.model';

describe('ServiceCardComponent', () => {
  let component: ServiceCardComponent;
  let fixture: ComponentFixture<ServiceCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceCardComponent);
    component = fixture.componentInstance;
    const serviceMock: ServiceModel = {
      id: 1,
      name: 'Test Service',
    } as ServiceModel;
    fixture.componentRef.setInput('service', serviceMock);
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
      component.onSelected.subscribe((v: unknown) => emitted.push(v));
      card = fixture.nativeElement.querySelector('.clickable-card');
    });

    it('is focusable and exposed as a button', () => {
      expect(card.getAttribute('tabindex')).toBe('0');
      expect(card.getAttribute('role')).toBe('button');
    });

    it('emits on click', () => {
      card.click();
      expect(emitted).toEqual([1]);
    });

    it('emits on Enter', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      expect(emitted).toEqual([1]);
    });

    it('emits on Space', () => {
      card.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));
      expect(emitted).toEqual([1]);
    });
  });
});

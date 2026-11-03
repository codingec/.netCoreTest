import { Component } from '@angular/core';
import { Location } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AppComponent } from './app.component';

@Component({ template: '' })
class StubPageComponent {}

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let router: Router;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([
          { path: 'auth/login', component: StubPageComponent },
          { path: 'booking/working-place', component: StubPageComponent },
          { path: 'booking/working-place/:id/services', component: StubPageComponent },
        ]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    router = TestBed.inject(Router);
    el = fixture.nativeElement;
  });

  async function navigate(url: string) {
    await fixture.ngZone!.run(() => router.navigateByUrl(url));
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('should create the app', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('hides the toolbar on auth pages', async () => {
    await navigate('/auth/login');
    expect(el.querySelector('mat-toolbar')).toBeNull();
  });

  it('shows the toolbar without a back button on the first booking step', async () => {
    await navigate('/booking/working-place');
    const toolbar = el.querySelector('mat-toolbar');
    expect(toolbar).not.toBeNull();
    expect(toolbar!.textContent).toContain('Booking');
    expect(el.querySelector('[aria-label="Go back"]')).toBeNull();
  });

  it('links the app name to the working-place list', async () => {
    await navigate('/booking/working-place');
    const brand = el.querySelector<HTMLAnchorElement>('a.brand');
    expect(brand?.getAttribute('href')).toBe('/booking/working-place');
  });

  it('shows a back button on later steps that goes back', async () => {
    await navigate('/booking/working-place/1/services');
    const location = TestBed.inject(Location);
    const backSpy = spyOn(location, 'back');
    const back = el.querySelector<HTMLButtonElement>('[aria-label="Go back"]');
    expect(back).not.toBeNull();
    back!.click();
    expect(backSpy).toHaveBeenCalled();
  });
});

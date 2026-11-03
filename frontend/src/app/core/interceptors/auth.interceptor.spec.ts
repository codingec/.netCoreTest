import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthStore } from '../state/auth.store';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let ctrl: HttpTestingController;
  let store: AuthStore;

  const session = (employeeId?: number) => ({
    code: 'OK',
    data: {
      token: 'abc',
      expiresAt: '',
      userId: '1',
      userName: 'u',
      roles: [],
      permissions: [],
      employeeId,
    },
  });

  beforeEach(() => {
    localStorage.removeItem('auth');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    ctrl = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    ctrl.verify();
    localStorage.removeItem('auth');
  });

  it('sends no Authorization header when logged out', () => {
    http.get(`${environment.apiUrl}/Office`).subscribe();
    const req = ctrl.expectOne(`${environment.apiUrl}/Office`);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('adds the bearer token to API requests when logged in', () => {
    store.setSession(session(5));
    http.get(`${environment.apiUrl}/Office`).subscribe();
    const req = ctrl.expectOne(`${environment.apiUrl}/Office`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush({});
  });

  it('does not send the token to other hosts', () => {
    store.setSession(session());
    http.get('https://placehold.co/150').subscribe();
    const req = ctrl.expectOne('https://placehold.co/150');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });
});

import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore.loadSession', () => {
  afterEach(() => localStorage.removeItem('auth'));

  it('restores token and employeeId after a reload', () => {
    localStorage.setItem(
      'auth',
      JSON.stringify({
        code: 'OK',
        data: {
          token: 't1',
          expiresAt: '',
          userId: '1',
          userName: 'u',
          roles: [],
          permissions: [],
          employeeId: 9,
        },
      })
    );
    const store = TestBed.inject(AuthStore);
    store.loadSession();
    expect(store.getToken()).toBe('t1');
    expect(store.getEmployeeId()).toBe(9);
  });

  it('clears a corrupted saved session instead of crashing', () => {
    localStorage.setItem('auth', '{not json');
    const store = TestBed.inject(AuthStore);
    store.loadSession();
    expect(store.getToken()).toBeNull();
    expect(localStorage.getItem('auth')).toBeNull();
  });
});

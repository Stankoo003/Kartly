import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

function setup(auth: Partial<AuthService>) {
  const router = { navigate: vi.fn() };
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      { provide: AuthService, useValue: auth },
      { provide: Router, useValue: router },
    ],
  });
  return {
    http: TestBed.inject(HttpClient),
    httpMock: TestBed.inject(HttpTestingController),
    router,
  };
}

describe('authInterceptor', () => {
  it('attaches the bearer token and logs out + redirects on a 401', () => {
    const logout = vi.fn();
    const { http, httpMock, router } = setup({ token: () => 'jwt-123', logout } as never);

    http.get('/api/products').subscribe({ next: () => {}, error: () => {} });

    const req = httpMock.expectOne('/api/products');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-123');
    req.flush('nope', { status: 401, statusText: 'Unauthorized' });

    expect(logout).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
    httpMock.verify();
  });

  it('leaves an anonymous 401 alone (no token → no redirect)', () => {
    const logout = vi.fn();
    const { http, httpMock, router } = setup({ token: () => null, logout } as never);

    http.get('/api/products').subscribe({ next: () => {}, error: () => {} });

    const req = httpMock.expectOne('/api/products');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush('nope', { status: 401, statusText: 'Unauthorized' });

    expect(logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
    httpMock.verify();
  });
});

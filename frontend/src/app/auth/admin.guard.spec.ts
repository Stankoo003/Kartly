import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { adminGuard } from './admin.guard';
import { AuthService } from './auth.service';

/** Runs adminGuard in an injection context against a stubbed AuthService/Router. */
function runGuard(auth: Partial<AuthService>) {
  const urlTree = {} as UrlTree;
  const router = { createUrlTree: vi.fn(() => urlTree) };
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthService, useValue: auth },
      { provide: Router, useValue: router },
    ],
  });
  const result = TestBed.runInInjectionContext(() =>
    adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );
  return { result, router, urlTree };
}

describe('adminGuard', () => {
  it('allows admins through', () => {
    const { result } = runGuard({ isAdmin: () => true, isAuthenticated: () => true } as never);
    expect(result).toBe(true);
  });

  it('redirects an authenticated non-admin to home', () => {
    const { result, router, urlTree } = runGuard({
      isAdmin: () => false,
      isAuthenticated: () => true,
    } as never);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/']);
    expect(result).toBe(urlTree);
  });

  it('redirects an anonymous visitor to login', () => {
    const { result, router, urlTree } = runGuard({
      isAdmin: () => false,
      isAuthenticated: () => false,
    } as never);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(result).toBe(urlTree);
  });
});

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Allows the route only for signed-in users with the Admin role. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAdmin()) return true;

  // Not an admin: send to login (or home if merely lacking the role).
  return router.createUrlTree([auth.isAuthenticated() ? '/' : '/login']);
};

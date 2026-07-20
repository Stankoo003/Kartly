import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Login } from './auth/login';
import { AdminProducts } from './admin/admin-products';
import { adminGuard } from './auth/admin.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: Login },
  { path: 'admin', component: AdminProducts, canActivate: [adminGuard] },
  { path: '**', redirectTo: '' },
];

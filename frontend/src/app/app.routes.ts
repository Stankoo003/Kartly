import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Login } from './auth/login';
import { AdminLayout } from './admin/admin-layout';
import { AdminProducts } from './admin/admin-products';
import { AdminUsers } from './admin/admin-users';
import { AdminSettings } from './admin/admin-settings';
import { adminGuard } from './auth/admin.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: Login },
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    canActivateChild: [adminGuard],
    children: [
      { path: '', redirectTo: 'products', pathMatch: 'full' },
      // `title` feeds the admin topbar heading (see AdminLayout.pageTitle).
      { path: 'products', component: AdminProducts, data: { title: 'Products' } },
      { path: 'users', component: AdminUsers, data: { title: 'Users' } },
      { path: 'settings', component: AdminSettings, data: { title: 'Settings' } },
    ],
  },
  { path: '**', redirectTo: '' },
];

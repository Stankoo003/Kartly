import { Routes } from '@angular/router';
import { Home } from './home/home';
import { ProductList } from './products/product-list';
import { Login } from './auth/login';
import { AdminLayout } from './admin/admin-layout';
import { AdminProducts } from './admin/admin-products';
import { AdminUsers } from './admin/admin-users';
import { AdminSettings } from './admin/admin-settings';
import { adminGuard } from './auth/admin.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductList },
  { path: 'login', component: Login },
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    canActivateChild: [adminGuard],
    children: [
      { path: '', redirectTo: 'products', pathMatch: 'full' },
      { path: 'products', component: AdminProducts },
      { path: 'users', component: AdminUsers },
      { path: 'settings', component: AdminSettings },
    ],
  },
  { path: '**', redirectTo: '' },
];

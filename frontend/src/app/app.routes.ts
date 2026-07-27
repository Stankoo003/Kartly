import { Routes } from '@angular/router';
import { Home } from './home/home';
import { ProductList } from './products/product-list';
import { ProductDetail } from './products/product-detail';
import { CartPage } from './cart/cart-page';
import { CheckoutPage } from './orders/checkout-page';
import { OrderConfirmation } from './orders/order-confirmation';
import { Login } from './auth/login';
import { adminGuard } from './auth/admin.guard';

// The admin components are deliberately NOT imported at the top of this file.
// A static import would bundle the whole panel — templates, field names, API
// paths — into main.js and ship it to every storefront visitor. They are loaded
// lazily below instead, so the code is only fetched once someone navigates to
// /admin and the guard has let them through.

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductList },
  { path: 'products/:slug', component: ProductDetail },
  { path: 'cart', component: CartPage },
  { path: 'checkout', component: CheckoutPage },
  { path: 'checkout/confirmation/:id', component: OrderConfirmation },
  { path: 'login', component: Login },
  {
    path: 'admin',
    loadComponent: () => import('./admin/admin-layout').then(m => m.AdminLayout),
    canActivate: [adminGuard],
    canActivateChild: [adminGuard],
    children: [
      { path: '', redirectTo: 'products', pathMatch: 'full' },
      {
        path: 'products',
        loadComponent: () => import('./admin/admin-products').then(m => m.AdminProducts),
      },
      {
        path: 'orders',
        loadComponent: () => import('./admin/admin-orders').then(m => m.AdminOrders),
      },
      { path: 'users', loadComponent: () => import('./admin/admin-users').then(m => m.AdminUsers) },
      {
        path: 'settings',
        loadComponent: () => import('./admin/admin-settings').then(m => m.AdminSettings),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

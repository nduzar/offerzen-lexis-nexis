import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'products',
    loadComponent: () => import('./pages/product-list.page').then((m) => m.ProductListPage)
  },
  {
    path: 'products/new',
    loadComponent: () => import('./pages/product-form.page').then((m) => m.ProductFormPage)
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./pages/product-form.page').then((m) => m.ProductFormPage)
  },
  {
    path: 'categories',
    loadComponent: () => import('./pages/categories.page').then((m) => m.CategoriesPage)
  }
];

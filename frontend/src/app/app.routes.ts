import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/log/log').then(m => m.LogComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./components/log/log').then(m => m.LogComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./components/reg/reg').then(m => m.RegisterComponent)
  },
  {
    path: 'home',
    loadComponent: () => import('./components/home/home').then(m => m.HomeComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];

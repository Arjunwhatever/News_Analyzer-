import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
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
    canActivate: [authGuard],
    loadComponent: () => import('./components/home/home').then(m => m.HomeComponent)
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];

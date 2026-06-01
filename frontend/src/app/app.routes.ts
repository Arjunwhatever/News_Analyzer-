import { Routes } from '@angular/router';

import { LogComponent } from './components/log/log';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/reg/reg';

export const routes: Routes = [
  {
    path: '',
    component: LogComponent
  },
  {
    path: 'login',
    component: LogComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'home',
    component: HomeComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];

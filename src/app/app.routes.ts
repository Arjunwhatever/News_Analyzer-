import { Routes } from '@angular/router';

import { LogComponent } from './components/log/log';
import { HomeComponent } from './components/home/home';

export const routes: Routes = [
  {
    path: '',
    component: LogComponent
  },
  {
    path: 'home',
    component: HomeComponent
  }
];

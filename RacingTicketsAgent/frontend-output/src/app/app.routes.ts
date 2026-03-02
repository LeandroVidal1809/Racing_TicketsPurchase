import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'partidos',
    loadComponent: () =>
      import('./features/partidos/partidos.component').then(m => m.PartidosComponent)
  },
  {
    path: 'compra/:id',
    loadComponent: () =>
      import('./features/compra/compra.component').then(m => m.CompraComponent)
  },
  {
    path: 'mi-cuenta',
    loadComponent: () =>
      import('./features/mi-cuenta/mi-cuenta.component').then(m => m.MiCuentaComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];

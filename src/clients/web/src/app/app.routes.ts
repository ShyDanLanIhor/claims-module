import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'claims' },
  {
    path: 'claims',
    loadComponent: () => import('./features/claims-list/claims-list').then((m) => m.ClaimsList),
    title: 'Claims',
  },
  {
    path: 'claims/new',
    loadComponent: () => import('./features/fnol-intake/fnol-intake').then((m) => m.FnolIntake),
    title: 'Log New Claim',
  },
  {
    path: 'claims/:id',
    loadComponent: () => import('./features/claim-detail/claim-detail').then((m) => m.ClaimDetail),
    title: 'Claim Detail',
  },
  { path: '**', redirectTo: 'claims' },
];

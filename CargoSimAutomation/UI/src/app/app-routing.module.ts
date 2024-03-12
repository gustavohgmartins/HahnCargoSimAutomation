import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { canDeactivateAdminGuard } from './features/admin/can-deactivate-admin.guard';

const routes: Routes = [
  {
    path:'',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path:'login',
    loadChildren: () => import('./features/public/public.module').then(x => x.PublicModule)
  },
  {
    path:'admin',
    loadChildren: () => import('./features/admin/admin.module').then(x => x.AdminModule),
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

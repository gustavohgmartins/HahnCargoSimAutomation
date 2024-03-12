import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminPageComponent } from './pages/admin-page/admin-page.component';
import { canDeactivateAdminGuard } from './can-deactivate-admin.guard';

const routes: Routes = [
  {
    path:'',
    component:AdminPageComponent,
    canDeactivate: [canDeactivateAdminGuard]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }

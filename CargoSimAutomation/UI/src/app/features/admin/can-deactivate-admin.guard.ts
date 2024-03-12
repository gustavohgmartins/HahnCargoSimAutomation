import { CanDeactivateFn } from '@angular/router';
import { AdminPageComponent } from './pages/admin-page/admin-page.component';

export const canDeactivateAdminGuard: CanDeactivateFn<AdminPageComponent> = (component: AdminPageComponent) => {
  if (!component._isRunning) {
    component.logout();
    return true;
  }

  const userConfirmed = window.confirm("Are you sure you want to exit the simulation? The simulation will be terminated.");
  if (userConfirmed) {
    component.logout();
  }
  return userConfirmed;

};

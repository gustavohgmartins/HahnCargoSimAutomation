import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminRoutingModule } from './admin-routing.module';
import { AdminPageComponent } from './pages/admin-page/admin-page.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { LogBoxComponent } from './components/log-box/log-box.component';


@NgModule({
  declarations: [
    AdminPageComponent,
    LogBoxComponent,
  ],
  imports: [
    SharedModule,
    CommonModule,
    AdminRoutingModule
  ],
  exports:[
    SharedModule,
  ]
})
export class AdminModule { }

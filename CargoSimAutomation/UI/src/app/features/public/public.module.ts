import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PublicRoutingModule } from './public-routing.module';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { SharedModule } from 'src/app/shared/shared.module';


@NgModule({
  declarations: [
    LoginPageComponent
  ],
  imports: [
    SharedModule,
    CommonModule,
    PublicRoutingModule
  ],
  exports:[
    SharedModule
  ]
})
export class PublicModule { }

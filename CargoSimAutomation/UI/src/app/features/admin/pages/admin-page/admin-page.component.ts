import { Component, inject } from '@angular/core';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { Auth } from 'src/app/domain/auth/auth.model';

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {

  user: any;

  authEndpoint = inject(AuthEndpoint);

  constructor() {
   }

  ngOnInit() {
  }
  
  startSimulation(){

  }
  stopSimulation(){

  }
}
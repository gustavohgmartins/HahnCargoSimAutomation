import { Component, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { Auth } from 'src/app/domain/auth/auth.model';
import { SimulationEndpoint } from 'src/app/domain/simulation/simulation.endpoint';

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {

  user: any;

  simulationEndpoint = inject(SimulationEndpoint);
  authEndpoint = inject(AuthEndpoint);

  private _snackBar = inject(MatSnackBar);

  constructor() {
  }

  ngOnInit() {
  }

  startSimulation() {
    this.simulationEndpoint.startSimulation().subscribe((response) => {
      this.openSnackBar("Simulation Started!")
    },
      (error) => {
        this.openSnackBar(error.error.message)
      })
  }

  stopSimulation() {
    this.simulationEndpoint.stopSimulation().subscribe((response) => {
      this.openSnackBar("Simulation Stopped!")
    },
      (error) => {
        this.openSnackBar(error.error.message)
      })
  }

  openSnackBar(message: string) {
    this._snackBar.open(message, '', {
      duration: 3000
    });
  }
}
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { SimulationEndpoint } from 'src/app/domain/simulation/simulation.endpoint';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { OrderEndpoint } from 'src/app/domain/order/order.endpoint';
import {MatDialogModule} from '@angular/material/dialog'

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {
  public username?: string;
  private _snackBar = inject(MatSnackBar);
  private connection: HubConnection;
  public logsData: { [key: string]: any[] } = {};
  public simData: { [key: string]: any[] } = {};

  authEndpoint = inject(AuthEndpoint);
  private readonly simulationEndpoint = inject(SimulationEndpoint);
  private readonly orderEndpoint = inject(OrderEndpoint);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly modalService = inject(MatDialogModule);

  constructor() {
    this.connection = new HubConnectionBuilder().withUrl('https://localhost:7051/AutomationHub').build();
    this.username = this.authEndpoint.authUserSig()?.Username;
  }

  async ngOnInit() {
    await this.ManageLogs();
  }

  startSimulation() {
    this.simulationEndpoint.startSimulation().subscribe((response) => {
      this.simData = {};
      this.logsData = {};
      localStorage.removeItem('simData');
      localStorage.removeItem('logsData');
      this.openSnackBar("Simulation Started")
    },
      (error) => {
        this.openSnackBar(error.error.message)
      })
  }

  stopSimulation() {
    this.simulationEndpoint.stopSimulation().subscribe((response) => {
      this.openSnackBar("Simulation Stopped")
    },
      (error) => {
        this.openSnackBar(error.error.message)
      })
  }

  generateOrders() {
    this.orderEndpoint.generate().subscribe((response) => {
      this.openSnackBar("Orders Generated")
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

  logout() {
    this.stopSimulation();
    this.clearStorage()
  }

  clearStorage(){
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('simData');
    localStorage.removeItem('logsData');
  }

  async ManageLogs() {
    this.connection.on('AutomationLogs', (username, entity, log) => {
      if (username == this.username) {
        if (entity == "Simulation") {
          if (!this.simData[entity]) {
            this.simData[entity] = [log];
          } else {
            this.simData[entity].push(log);
          }
        } else {
          if (!this.logsData[entity]) {
            this.logsData[entity] = [log];
          } else {
            this.logsData[entity].push(log);
          }
        }

        localStorage.setItem('simData', JSON.stringify(this.simData));
        localStorage.setItem('logsData', JSON.stringify(this.logsData));

        this.cdRef.markForCheck();
      }

    });

    try {
      await this.connection.start();
      console.log("Connected to AutomationHub")

      const simDataFromStorage = localStorage.getItem('simData');
      const logsDataFromStorage = localStorage.getItem('logsData');

      if (simDataFromStorage) {
        this.simData = JSON.parse(simDataFromStorage);
      }

      if (logsDataFromStorage) {
        this.logsData = JSON.parse(logsDataFromStorage);
      }
    } catch (e) {
      console.log("Failed to connect to automationHub", e);
    }
  }
}

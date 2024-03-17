import { ChangeDetectorRef, Component, HostListener, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { SimulationEndpoint } from 'src/app/domain/simulation/simulation.endpoint';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { OrderEndpoint } from 'src/app/domain/order/order.endpoint';
import { MatDialog, MatDialogModule } from '@angular/material/dialog'
import { Router } from '@angular/router';
import { Environment } from 'src/environments/environment';

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {
  public _username?: string;
  private _snackBar = inject(MatSnackBar);
  private _connection: HubConnection;
  public _logsData: { [key: string]: any[] } = {};
  public _simData: { [key: string]: any[] } = {};
  public _isRunning?: boolean;
  public _coins: number = 0;
  public _transporters: number = 0;

  authEndpoint = inject(AuthEndpoint);
  private readonly simulationEndpoint = inject(SimulationEndpoint);
  private readonly orderEndpoint = inject(OrderEndpoint);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly environment = inject(Environment);

  constructor() {
    this._connection = new HubConnectionBuilder().withUrl(this.environment.hubEndpoint).build();
    this._username = this.authEndpoint.authUserSig()?.Username;
  }

  @HostListener('window:beforeunload', ['$event'])
  public beforeunloadHandler(event: any) {
  }

  async ngOnInit() {
    await this.ManageLogs();
  }

  startSimulation() {
    if (this._connection.state != "Connected") {
      this._connection.start();
    }

    if (this._isRunning) {
      return;
    }

    this.simulationEndpoint.startSimulation().subscribe((response) => {
      this.openSnackBar("Simulation Started")
    },
      (error) => {
        this.openSnackBar(error.error.message)
      })
  }

  stopSimulation() {
    if (!this._isRunning) {
      return;
    }

    this.simulationEndpoint.stopSimulation().subscribe(async (response) => {
      this.openSnackBar("Simulation Stopped")

    },
      (error) => {
        let err = error.error?.message
        this.openSnackBar(err ?? "Unexpected error");
      })
  }

  generateOrders() {
    this.orderEndpoint.generate().subscribe((response) => {
      this.openSnackBar("Orders Generated")
    },
      (error) => {
        let err = error.error?.message
        this.openSnackBar(err ?? "Unexpected error");
      })
  }

  openSnackBar(message: string) {
    this._snackBar.open(message, '', {
      duration: 3000
    });
  }

  async logout() {
    this.stopSimulation();
    this.clearUserAuth();
  }

  clearLogs() {
    this._simData = { ["Simulation"]: ["Waiting..."] }
    this._logsData = {};
    this._transporters = 0;
    this._coins = 0;
    localStorage.removeItem(this._username + 'transporters');
    localStorage.removeItem(this._username + 'simData');
    localStorage.removeItem(this._username + 'logsData');
    localStorage.removeItem(this._username + 'coins');
  }

  clearUserAuth() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  async ManageLogs() {
    this._connection.on('AutomationLogs', (username, entity, log) => {
      if (username == this._username) {
        switch (entity) {
          case "isRunning":
            this._isRunning = Number(log) ? true : false;
            break;
          case "transporters":
            this._transporters = Number(log)
            break;
          case "coins":
            this._coins = Number(log)
            break;
          case "Simulation":
            if (!this._simData[entity]) {
              this._simData[entity] = [log];
            } else {
              this._simData[entity].push(log);
            }
            break;
          default:
            if (!this._logsData[entity]) {
              this._logsData[entity] = [log];
            } else {
              this._logsData[entity].push(log);
            }
        }

        localStorage.setItem(username + 'isRunning', JSON.stringify(this._isRunning));
        localStorage.setItem(username + 'coins', JSON.stringify(this._coins));
        localStorage.setItem(username + 'transporters', JSON.stringify(this._transporters));
        localStorage.setItem(username + 'simData', JSON.stringify(this._simData));
        localStorage.setItem(username + 'logsData', JSON.stringify(this._logsData));
      }

      this.cdRef.markForCheck();
    });

    try {
      await this._connection.start();
      console.log("Connected to AutomationHub")

      const isRunning = localStorage.getItem(this._username + 'isRunning');
      const coins = localStorage.getItem(this._username + 'coins');
      const transporters = localStorage.getItem(this._username + 'transporters');
      const simDataFromStorage = localStorage.getItem(this._username + 'simData');
      const logsDataFromStorage = localStorage.getItem(this._username + 'logsData');

      if (isRunning) {
        this._isRunning = JSON.parse(isRunning);
      }

      if (coins) {
        this._coins = JSON.parse(coins);
      }

      if (transporters) {
        this._transporters = JSON.parse(transporters);
      }

      if (simDataFromStorage) {
        this._simData = JSON.parse(simDataFromStorage);
      }
      else {
        this._simData = { ["Simulation"]: ["Waiting..."] }
      }

      if (logsDataFromStorage) {
        this._logsData = JSON.parse(logsDataFromStorage);
      }
    } catch (e) {
      console.log("Failed to connect to automationHub", e);
    }

    if(!this._isRunning){
      this._isRunning = false;
    }
  }
}

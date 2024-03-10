import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { SimulationEndpoint } from 'src/app/domain/simulation/simulation.endpoint';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { OrderEndpoint } from 'src/app/domain/order/order.endpoint';
@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {
  private user: any;
  private _snackBar = inject(MatSnackBar);
  private connection: HubConnection;
  public logsData: { [key: string]: any[] } = {};

  authEndpoint = inject(AuthEndpoint);
  private readonly simulationEndpoint = inject(SimulationEndpoint);
  private readonly orderEndpoint = inject(OrderEndpoint);
  private readonly cdRef = inject(ChangeDetectorRef);

  constructor() {
    this.connection = new HubConnectionBuilder().withUrl('https://localhost:7051/AutomationHub').build();
  }

  async ngOnInit() {
    await this.ManageLogs();
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

  generateOrders() {
    this.orderEndpoint.generate().subscribe((response) => {
      this.openSnackBar("Orders Generated!")
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

  async ManageLogs() {
    this.connection.on('AutomationLogs', (entity, log) => {
      if (!this.logsData[entity]) {
        this.logsData[entity] = [log];
      } else {
        this.logsData[entity].push(log);
      }
      // Chame markForCheck() para notificar o Angular de mudanças
      this.cdRef.markForCheck();
    });

    try {
      await this.connection.start();
      console.log("Connected to AutomationHub")
    } catch (e) {
      console.log("Failed to connect to automationHub", e);
    }
  }
}



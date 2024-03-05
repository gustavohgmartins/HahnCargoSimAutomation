import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { Auth } from 'src/app/domain/auth/auth.model';
import { Environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SimulationEndpoint {

  get endpoint(): string{
    return `${this.environment.apiEndpoint}/simulation`
  }

  authUserSig = signal<Auth | null | undefined>(undefined);
  
  constructor(private readonly http: HttpClient, private environment: Environment) { }

  startSimulation(){
    return this.http.post<any>(`${this.endpoint}/start`,{});
  }

  stopSimulation(){
    return this.http.post<any>(`${this.endpoint}/stop`,{});
  }
}

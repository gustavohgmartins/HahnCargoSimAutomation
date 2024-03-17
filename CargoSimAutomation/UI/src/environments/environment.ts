import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
  })

export class Environment{
    private baseUrl = "http://localhost:5000";

    get apiEndpoint(): string{
        return this.baseUrl;
    }

    get hubEndpoint(): string{
        return this.baseUrl + "/AutomationHub";
    }
}
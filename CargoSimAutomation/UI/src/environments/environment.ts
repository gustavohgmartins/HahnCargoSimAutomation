import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
  })

export class Environment{
    private baseUrl = "https://localhost:7051";

    get apiEndpoint(): string{
        return this.baseUrl;
    }
}
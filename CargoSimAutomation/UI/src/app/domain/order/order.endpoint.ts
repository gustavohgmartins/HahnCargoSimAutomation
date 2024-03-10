import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderEndpoint {

  get endpoint(): string{
    return `${this.environment.apiEndpoint}/order`
  }
  
  constructor(private readonly http: HttpClient, private environment: Environment) { }

  generate(){
    return this.http.post<any>(`${this.endpoint}/generate`,{});
  }
}

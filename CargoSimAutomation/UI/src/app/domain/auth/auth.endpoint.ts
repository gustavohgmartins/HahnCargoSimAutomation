import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { Auth } from 'src/app/domain/auth/auth.model';
import { Environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthEndpoint {

  get endpoint(): string{
    return `${this.environment.apiEndpoint}/auth`
  }

  authUserSig = signal<Auth | null | undefined>(undefined);
  
  constructor(private readonly http: HttpClient, private environment: Environment) { }

  login(auth: Auth){
    return this.http.post<Auth>(`${this.endpoint}/Login`, auth);
  }

  verifyLogin(){
    return this.http.get<Auth>(`${this.endpoint}/VerifyLogin`);
  }
}

import { Component, OnInit, inject } from '@angular/core';
import { AuthEndpoint } from './domain/auth/auth.endpoint';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'cargo-sim-automation';
  authenticating: boolean = false;
  private readonly router = inject(Router);
  private readonly authEndpoint = inject(AuthEndpoint);

  ngOnInit(): void {
    this.authenticating = true;
    this.authEndpoint.validateLogin().subscribe((response) => {
      let localUser = localStorage.getItem('user');
      if (localUser) {
        this.authEndpoint.authUserSig.set(JSON.parse(localUser));
      }
      this.authenticating = false;
      this.router.navigate(['./admin']);
    },
      (error) => {
        console.log(error)
        this.authEndpoint.authUserSig.set(null);
        this.authenticating = false;
        this.router.navigate(['']);
      })
  }
}

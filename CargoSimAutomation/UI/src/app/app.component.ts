import { Component, OnInit, inject } from '@angular/core';
import { AuthEndpoint } from './domain/auth/auth.endpoint';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit{
  title = 'cargo-sim-automation';

  private readonly router = inject(Router);
  private readonly authEndpoint = inject(AuthEndpoint);

  ngOnInit(): void {
    this.authEndpoint.verifyLogin().subscribe((response) => {
      this.authEndpoint.authUserSig.set(response);
      console.log(this.authEndpoint.authUserSig())
      this.router.navigate(['./admin']);
    },
    (error) => {
      console.log(error)
      this.authEndpoint.authUserSig.set(null);
      localStorage.setItem("token", '');
      this.router.navigate(['./login']);
    })
  }
}

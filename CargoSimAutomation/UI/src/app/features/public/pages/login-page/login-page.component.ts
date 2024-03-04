import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from 'src/app/domain/auth/auth.model';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.css']
})
export class LoginPageComponent {

  myForm: FormGroup = new FormGroup({});
  loading: boolean = false;


  private readonly router = inject(Router);
  private readonly authEndpoint = inject(AuthEndpoint);

  constructor() { }

  ngOnInit() {
    this.createFormGroup();
  }

  createFormGroup() {
    this.myForm.addControl("username", new FormControl(null, [Validators.required]));
    this.myForm.addControl("password", new FormControl(null, [Validators.required]));
  }

  private bodyBuilder(): Auth {
    return this.myForm.getRawValue();
  }

  onSubmit() {
    if (this.myForm.valid) {
      this.loading = true;
      this.authEndpoint.login(this.bodyBuilder()).subscribe((response) => {
        localStorage.setItem('token', response.Token);
        localStorage.setItem('user', JSON.stringify(response));
        this.authEndpoint.authUserSig.set(response);
        this.loading = false;
        this.router.navigate(['./admin'])
      },
        (error) => {
          this.myForm.get('password')!.setErrors({ loginFailed: true });
          this.loading = false;
          console.log(error)
        })
    }
  }
}

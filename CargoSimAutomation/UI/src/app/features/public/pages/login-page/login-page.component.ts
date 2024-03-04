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

  private bodyBuilder(): Auth{
    return this.myForm.getRawValue();
  }

  onSubmit() {
    if (this.myForm.valid) {
      this.authEndpoint.login(this.bodyBuilder()).subscribe((response) => {
        this.authEndpoint.authUserSig.set(response);
        this.router.navigate(['./admin'])
      },
      (error) => {
        this.myForm.get('password')!.setErrors({ loginFailed: true });
        console.log(error)
      })
    }
  }
}

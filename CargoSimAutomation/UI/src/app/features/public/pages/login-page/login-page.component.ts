import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from 'src/app/domain/auth/auth.model';
import { AuthEndpoint } from 'src/app/domain/auth/auth.endpoint';
import { MatSnackBar } from '@angular/material/snack-bar';

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
  private _snackBar = inject(MatSnackBar);
  
  constructor() {}

  ngOnInit() {
    this.createFormGroup();
  }

  
  onSubmit() {
    if (this.myForm.valid) {
      this.loading = true;
      this.authEndpoint.login(this.bodyBuilder()).subscribe((response) => {
        this.openSnackBar("Successfully logged in!")
        localStorage.setItem('token', response.Token);
        localStorage.setItem('user', JSON.stringify(response));
        this.authEndpoint.authUserSig.set(response);
        this.loading = false;
        this.router.navigate(['./admin'])
      },
        (error) => {
          if(error.error.message){
            this.myForm.get('password')!.setErrors({ loginFailed: true });
            this.openSnackBar(error.error.message)
          }
          else{
            this.openSnackBar("Unexpected error")
          }
          this.loading = false;
        })
      }
    }
    createFormGroup() {
      this.myForm.addControl("username", new FormControl(null, [Validators.required]));
      this.myForm.addControl("password", new FormControl(null, [Validators.required]));
    }
  
    
    openSnackBar(message: string) {
      this._snackBar.open(message, 'Ok',{
        duration: 3000
      });
    }
    
    private bodyBuilder(): Auth {
      return this.myForm.getRawValue();
    }
}

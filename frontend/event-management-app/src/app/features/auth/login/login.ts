import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent {
  loginForm: FormGroup;
  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.router.navigate(['/events']);
      },
      error: (error) => {
        this.errorMessage.set(error.error?.message || 'Invalid credentials');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });    
  }
}

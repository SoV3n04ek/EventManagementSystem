import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class RegisterComponent {
  registerForm: FormGroup;
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  
  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      name: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.pattern(/^[a-zA-Z\s]+$/)
      ]],
      email: ['', [Validators.required, Validators.email]],
        password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }
  
  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value != confirmPassword.value)
    {
      return { passwordMismatch: true };
    }
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const { confirmPassword, ...registerData } = this.registerForm.value;

    this.authService.register(registerData).subscribe({
      next: () => {
        this.successMessage.set('Account created successfully! Redirecting to login...');
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error) => {
        this.errorMessage.set(
          error.error?.message ||
          error.error?.error ||
          'Registration failed. Please try again.'
        );
        this.loading.set(false);
      }
    });
  }
}

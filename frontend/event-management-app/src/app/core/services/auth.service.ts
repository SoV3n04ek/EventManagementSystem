import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../../models/user';
import { TokenService  } from './token.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  
  currentUser = signal<User | null>(null);

  constructor(
    private http: HttpClient,
    private tokenService: TokenService,
    private router: Router
  ) {
    this.loadCurrentUser();
  }

  register(data: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  login(credentials: LoginRequest): Observable<{data : AuthResponse}> {
    return this.http.post<{ data: AuthResponse }>(`${this.apiUrl}/login`, credentials)
      .pipe(
        tap(response => {
          this.tokenService.setToken(response.data.token);
          this.currentUser.set(response.data.user);
        })
      )

  }

  logout(): void {
    this.tokenService.removeToken();
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private loadCurrentUser(): void {
    if (this.tokenService.isAuthenticated()) {
      this.http.get<User>(`${this.apiUrl}/me`).subscribe({
        next: (user) => this.currentUser.set(user),
        error: () => this.logout()
      });
    }
  }
}

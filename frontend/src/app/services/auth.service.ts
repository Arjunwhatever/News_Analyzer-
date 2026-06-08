import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  constructor(private http: HttpClient, private router: Router) {}

  /** POST /api/auth/login — sets HTTP-Only cookies automatically. */
  login(username: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/login`, { username, password });
  }

  /** POST /api/auth/register — creates a new user account. */
  register(username: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, { username, password });
  }

  /** Remove cookies via backend and navigate to the login page. */
  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe(() => {
      this.router.navigate(['/']);
    });
  }

  /** Quick check — true when the isAuthenticated cookie exists. */
  isLoggedIn(): boolean {
    return document.cookie.includes('isAuthenticated=true');
  }
}

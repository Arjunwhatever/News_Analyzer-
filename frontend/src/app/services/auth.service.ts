import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  constructor(private http: HttpClient, private router: Router) {}

  /** POST /api/auth/login — returns a raw JWT string. */
  login(username: string, password: string): Observable<string> {
    return this.http
      .post(`${this.apiUrl}/login`, { username, password }, { responseType: 'text' })
      .pipe(tap(token => localStorage.setItem('token', token)));
  }

  /** POST /api/auth/register — creates a new user account. */
  register(username: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, { username, password });
  }

  /** Remove the stored token and navigate to the login page. */
  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/']);
  }

  /** Retrieve the stored JWT (or null if not logged in). */
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  /** Quick check — true when a token exists in storage. */
  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}

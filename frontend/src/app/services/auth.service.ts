import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  constructor(private http: HttpClient, private router: Router) {}

  // Sends the user's credentials to the backend. If successful, the backend automatically sets our secure session cookies!
  // Note: We don't need to manually save any tokens here anymore.
  login(username: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/login`, { username, password });
  }

  // Registers a brand new account and saves it to the database.
  register(username: string, password: string, preferredTopics?: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, { username, password, preferredTopics });
  }

  // Tells the backend to destroy our session cookies, then kicks the user back to the login screen.
  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe(() => {
      this.router.navigate(['/']);
    });
  }

  // A quick, synchronous check to see if the user is currently logged in.
  // It specifically looks for the non-HttpOnly 'isAuthenticated' cookie we set during login.
  isLoggedIn(): boolean {
    return document.cookie.includes('isAuthenticated=true');
  }

  getPreferences(): Observable<{topics: string}> {
    return this.http.get<{topics: string}>(`${this.apiUrl}/preferences`);
  }

  updatePreferences(topics: string): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/preferences`, { topics });
  }
}

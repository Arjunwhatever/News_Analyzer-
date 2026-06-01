import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-log',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, HttpClientModule],
  templateUrl: './log.html',
  styleUrls: ['./log.scss']
})
export class LogComponent {

  username = '';
  password = '';
  error: string | null = null;
  isLoading = false;
  tickerAnimated = true;

  categories = [
    'FACTS UNFILTERED',
    'MEDIA UNFILTERED',
    'VOICES UNFILTERED',
    'STORIES UNFILTERED',
    'ANALYSIS UNFILTERED',
    'REPORTING UNFILTERED',
    'COVERAGE UNFILTERED',
    'INFORMATION UNFILTERED',
    'PEOPLE UNFILTERED'
  ];

  private apiUrl = 'https://localhost:7121/api/auth';

  constructor(private http: HttpClient, private router: Router) {}

  onLogin(): void {
    if (!this.username.trim() || !this.password.trim() || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.http.post(`${this.apiUrl}/login`, {
      username: this.username.trim(),
      password: this.password
    }, { responseType: 'text' }).subscribe({
      next: (token) => {
        localStorage.setItem('token', token);
        this.isLoading = false;
        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.error = err.error || 'Invalid username or password.';
        this.isLoading = false;
      }
    });
  }
}

import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-log',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './log.html',
  styleUrls: ['./log.scss']
})
export class LogComponent {

  username = '';
  password = '';
  error: string | null = null;
  isLoading = false;
  tickerAnimated = true;

  private destroyRef = inject(DestroyRef);

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

  constructor(private authService: AuthService, private router: Router) {}

  onLogin(): void {
    if (!this.username.trim() || !this.password.trim() || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.authService.login(this.username.trim(), this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
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

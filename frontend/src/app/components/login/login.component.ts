import { Component, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-log',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  username = '';
  password = '';
  error: string | null = null;
  isLoading = false;
  tickerAnimated = true;

  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

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

  private authService = inject(AuthService);
  private router = inject(Router);

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
          this.cdr.detectChanges();
          this.router.navigate(['/home']);
        },
        error: (err) => {
          this.error = err.error || 'Invalid username or password.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }
}

// src/app/components/register/register.component.ts
import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reg.html',
  styleUrls: ['./reg.scss']
})
export class RegisterComponent {
  email: string = '';
  password: string = '';
  confirmPassword: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';

  strengthPercent: number = 0;
  strengthColor: string = '#4a4a45';
  strengthLabel: string = '';

  private destroyRef = inject(DestroyRef);

  private authService = inject(AuthService);
  private router = inject(Router);

  register() {
    this.errorMessage = '';

    // Basic validation
    if (!this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'All fields are required.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.password.length < 8) {
      this.errorMessage = 'Password must be at least 8 characters.';
      return;
    }

    const score = this.getScore(this.password);
    if (score < 3) {
      this.errorMessage = 'Password is too weak. Mix letters, numbers, and symbols.';
      return;
    }

    this.isLoading = true;

    this.authService.register(this.email, this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Registration successful — go to login
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 409) {
            this.errorMessage = 'Account already exists.';
          } else {
            this.errorMessage = 'Something went wrong. Please try again.';
          }
        }
      });
  }

  onPasswordChange() {
    this.calculateStrength(this.password);
  }

  private getScore(password: string): number {
    if (!password) return 0;
    let score = 0;
    if (password.length >= 8) score += 1;
    if (/[A-Z]/.test(password)) score += 1;
    if (/[a-z]/.test(password)) score += 1;
    if (/[0-9]/.test(password)) score += 1;
    if (/[^A-Za-z0-9]/.test(password)) score += 1;
    return score;
  }

  private calculateStrength(password: string) {
    const score = this.getScore(password);
    if (!password) {
      this.strengthPercent = 0;
      this.strengthLabel = '';
      return;
    }

    this.strengthPercent = (score / 5) * 100;

    if (score <= 2) {
      this.strengthColor = '#b87070'; // Weak
      this.strengthLabel = 'Weak';
    } else if (score === 3 || score === 4) {
      this.strengthColor = '#d4b483'; // Fair
      this.strengthLabel = 'Fair';
    } else if (score === 5) {
      this.strengthColor = '#8ab870'; // Strong
      this.strengthLabel = 'Strong';
    }
  }
}
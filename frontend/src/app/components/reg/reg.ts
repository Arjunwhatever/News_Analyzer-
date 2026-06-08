// src/app/components/register/register.component.ts
import { Component, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
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

  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

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

    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters.';
      return;
    }

    this.isLoading = true;

    this.authService.register(this.email, this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Registration successful — go to login
          this.isLoading = false;
          this.cdr.detectChanges();
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 400) {
            this.errorMessage = err.error || 'Invalid format.';
          } else if (err.status === 409) {
            this.errorMessage = 'Account already exists.';
          } else {
            this.errorMessage = 'Something went wrong. Please try again.';
          }
          this.cdr.detectChanges();
        }
      });
  }
}
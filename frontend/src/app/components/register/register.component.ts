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
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
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

    // Quick sanity checks before we bother the backend
    // Basic validation
    if (!this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'All fields are required.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_\-+=]).{8,}$/;
    if (!passwordRegex.test(this.password)) {
      this.errorMessage = 'Password must be at least 8 characters and contain uppercase, lowercase, number, and special character.';
      return;
    }

    this.isLoading = true;

    // Everything looks good locally, let's ask the API to create the account!
    this.authService.register(this.email, this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Awesome, the account was created successfully! Send them back to log in.
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
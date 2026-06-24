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
  showTopicsModal = false;
  availableTopics: string[] = ['Technology', 'Business', 'Politics', 'Science', 'Sports', 'Entertainment', 'Health', 'AI', 'Space'];
  selectedTopics: Set<string> = new Set<string>();
  customTopic: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';

  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

  private authService = inject(AuthService);
  private router = inject(Router);

  toggleTopic(topic: string) {
    if (this.selectedTopics.has(topic)) {
      this.selectedTopics.delete(topic);
    } else {
      this.selectedTopics.add(topic);
    }
  }

  addCustomTopic() {
    const topic = this.customTopic.trim();
    if (topic) {
      // Capitalize first letter for consistency
      const formattedTopic = topic.charAt(0).toUpperCase() + topic.slice(1);
      if (!this.availableTopics.includes(formattedTopic)) {
        this.availableTopics.push(formattedTopic);
      }
      this.selectedTopics.add(formattedTopic);
      this.customTopic = '';
    }
  }

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

    const preferredTopicsStr = Array.from(this.selectedTopics).join(', ');

    // Everything looks good locally, let's ask the API to create the account!
    this.authService.register(this.email, this.password, preferredTopicsStr)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Awesome, the account was created successfully! Now automatically log them in.
          this.authService.login(this.email, this.password)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: () => {
                this.isLoading = false;
                this.cdr.detectChanges();
                this.router.navigate(['/feed']);
              },
              error: (err) => {
                // If auto-login fails for some reason, just send them to login screen
                this.isLoading = false;
                this.cdr.detectChanges();
                this.router.navigate(['/login']);
              }
            });
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
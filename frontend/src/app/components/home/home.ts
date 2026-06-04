import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { AnalysisService } from '../../services/analysis.service';
import { AnalysisResult } from '../../models/analysis-result';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent {
  inputText: string = '';
  isLoading: boolean = false;
  result: AnalysisResult | null = null;
  error: string | null = null;

  constructor(
    private authService: AuthService,
    private analysisService: AnalysisService,
    private router: Router
  ) {}

  logout() {
    this.authService.logout();
  }

  get isUrl(): boolean {
    try {
      new URL(this.inputText.trim());
      return true;
    } catch {
      return false;
    }
  }

  get inputIsValid(): boolean {
    return this.inputText.trim().length > 10;
  }

  get biasColor(): string {
    if (!this.result) return '#888';
    const score = this.result.bias_score;
    if (score < -4) return '#4a90d9';      // blue for left
    if (score > 4) return '#c0392b';       // red for right
    return '#8a8a6a';                       // neutral
  }

  get biasPosition(): number {
    if (!this.result) return 50;
    // Map -10..10 to 0..100%
    return ((this.result.bias_score + 10) / 20) * 100;
  }

  get biasDescription(): string {
    if (!this.result) return '';
    const s = this.result.bias_score;
    if (s <= -7) return 'strongly left';
    if (s <= -4) return 'left leaning';
    if (s <= -1) return 'slightly left';
    if (s < 1)  return 'center / neutral';
    if (s < 4)  return 'slightly right';
    if (s < 7)  return 'right leaning';
    return 'strongly right';
  }

  analyze() {
    if (!this.inputIsValid || this.isLoading) return;

    this.isLoading = true;
    this.result = null;
    this.error = null;

    this.analysisService.analyze(this.inputText).subscribe({
      next: (data) => {
        this.result = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || err?.error || 'Something went wrong. Please try again.';
        this.isLoading = false;
      }
    });
  }

  reset() {
    this.inputText = '';
    this.result = null;
    this.error = null;
  }
}

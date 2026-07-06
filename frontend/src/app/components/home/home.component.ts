import { Component, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../services/auth.service';
import { AnalysisService, SourceBiasStat } from '../../services/analysis.service';
import { FeedService } from '../../services/feed.service';
import { AnalysisResult } from '../../models/analysis-result';
import { LiveNewsArticle } from '../../models/live-news-article';

export interface RecommendationResponse {
  averageBiasScore: number;
  message: string;
  articles: LiveNewsArticle[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  inputText: string = '';
  isLoading: boolean = false;
  result: AnalysisResult | null = null;
  error: string | null = null;

  sourceStats: SourceBiasStat[] = [];
  isLoadingStats: boolean = false;

  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

  private authService = inject(AuthService);
  private analysisService = inject(AnalysisService);
  private feedService = inject(FeedService);
  private router = inject(Router);

  recommendationsData: RecommendationResponse | null = null;

  logout() {
    this.authService.logout();
  }

  ngOnInit() {
    this.fetchSourceStats();
  }

  fetchSourceStats() {
    this.isLoadingStats = true;
    this.analysisService.getSourceBiasStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stats) => {
          this.sourceStats = stats.filter(s => s.sourceName !== 'Direct Text');
          this.isLoadingStats = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to fetch source stats', err);
          this.isLoadingStats = false;
          this.cdr.detectChanges();
        }
      });
  }

  get isUrl(): boolean {
    try {
      new URL(this.inputText.trim());
      return true;
    } catch {
      return false;
    }
  }

  // Makes sure the user didn't just type gibberish before we send it off
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

  // Calculates where the little tick mark should sit on the slider bar based on the score!
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

  getSourceBiasColor(score: number): string {
    if (score < -4) return '#4a90d9';
    if (score > 4) return '#c0392b';
    return '#8a8a6a';
  }

  getSourceBiasPosition(score: number): number {
    return ((score + 10) / 20) * 100;
  }

  getSourceBiasDescription(s: number): string {
    if (s <= -7) return 'strongly left';
    if (s <= -4) return 'left leaning';
    if (s <= -1) return 'slightly left';
    if (s < 1)  return 'center / neutral';
    if (s < 4)  return 'slightly right';
    if (s < 7)  return 'right leaning';
    return 'strongly right';
  }

  // The big green button! Fires off the text to the backend and waits for the AI's verdict.
  analyze() {
    if (!this.inputIsValid || this.isLoading) return;

    this.isLoading = true;
    this.result = null;
    this.error = null;

    this.analysisService.analyze(this.inputText)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          // Boom, we got the results! Update the UI to show the fancy charts.
          this.result = data;
          this.isLoading = false;
          this.fetchSourceStats(); // Refresh the dashboard
          this.fetchRecommendations();
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.error = err?.error?.message || err?.error || 'Something went wrong. Please try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  fetchRecommendations() {
    let topicsParam: string | undefined = undefined;
    
    // Extract highly specific keywords (topics) detected in the currently analyzed article.
    // These topics are passed to the backend to ensure recommendations are about the same subject matter,
    // rather than just random news from opposite-leaning sources.
    if (this.result && this.result.topics && this.result.topics.length > 0) {
      topicsParam = this.result.topics.join(',');
    }

    this.feedService.getRecommendations(topicsParam)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.recommendationsData = data;
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Failed to fetch recommendations', err)
      });
  }

  reset() {
    this.inputText = '';
    this.result = null;
    this.error = null;
    this.recommendationsData = null;
  }
}

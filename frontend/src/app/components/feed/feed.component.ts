import { Component, OnInit, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../services/auth.service';
import { FeedService } from '../../services/feed.service';
import { AnalysisService } from '../../services/analysis.service';
import { LiveNewsArticle } from '../../models/live-news-article';
import { AnalysisResult } from '../../models/analysis-result';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './feed.component.html',
  styleUrls: ['./feed.component.scss']
})
export class FeedComponent implements OnInit {
  articles: LiveNewsArticle[] = [];
  isLoadingNews: boolean = true;
  errorNews: string | null = null;

  // Track analysis state for each article by URL
  analyzingUrls: Set<string> = new Set();
  analysisResults: Map<string, AnalysisResult> = new Map();
  analysisErrors: Map<string, string> = new Map();

  // Preferences state
  showPreferencesModal = false;
  preferencesInput = '';
  isSavingPreferences = false;

  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);
  
  private authService = inject(AuthService);
  private feedService = inject(FeedService);
  private analysisService = inject(AnalysisService);
  private router = inject(Router);

  ngOnInit() {
    this.fetchNews();
    this.loadPreferences();
  }

  loadPreferences() {
    this.authService.getPreferences()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.preferencesInput = res.topics || '';
          this.cdr.detectChanges();
        }
      });
  }

  openPreferences() {
    this.showPreferencesModal = true;
  }

  closePreferences() {
    this.showPreferencesModal = false;
  }

  savePreferences() {
    this.isSavingPreferences = true;
    this.authService.updatePreferences(this.preferencesInput)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSavingPreferences = false;
          this.showPreferencesModal = false;
          this.fetchNews(); // Refresh feed with new topics
        },
        error: () => {
          this.isSavingPreferences = false;
          this.cdr.detectChanges();
        }
      });
  }

  fetchNews() {
    this.isLoadingNews = true;
    this.errorNews = null;
    this.feedService.getLiveNews()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.articles = data;
          this.isLoadingNews = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.errorNews = err?.error?.message || err?.message || 'Failed to load live news.';
          this.isLoadingNews = false;
          this.cdr.detectChanges();
        }
      });
  }

  analyzeArticle(url: string) {
    if (!url || this.analyzingUrls.has(url)) return;

    this.analyzingUrls.add(url);
    this.analysisErrors.delete(url);
    this.cdr.detectChanges();

    this.analysisService.analyze(url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.analysisResults.set(url, result);
          this.analyzingUrls.delete(url);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.analysisErrors.set(url, err?.error?.message || 'Analysis failed.');
          this.analyzingUrls.delete(url);
          this.cdr.detectChanges();
        }
      });
  }

  getBiasColor(score: number): string {
    if (score < -4) return '#4a90d9';      // blue for left
    if (score > 4) return '#c0392b';       // red for right
    return '#8a8a6a';                       // neutral
  }

  getBiasPosition(score: number): number {
    return ((score + 10) / 20) * 100;
  }

  logout() {
    this.authService.logout();
  }
}

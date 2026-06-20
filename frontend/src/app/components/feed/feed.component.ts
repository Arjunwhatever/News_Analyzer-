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
  availableTopics: string[] = ['Technology', 'Business', 'Politics', 'Science', 'Sports', 'Entertainment', 'Health', 'AI', 'Space'];
  selectedTopics: Set<string> = new Set<string>();
  customTopic: string = '';
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
          this.selectedTopics.clear();
          if (res.topics) {
            res.topics.split(',').map(t => t.trim()).filter(t => t).forEach(t => this.selectedTopics.add(t));
          }
          this.cdr.detectChanges();
        }
      });
  }

  openPreferences() {
    this.showPreferencesModal = true;
  }

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
      const formattedTopic = topic.charAt(0).toUpperCase() + topic.slice(1);
      if (!this.availableTopics.includes(formattedTopic)) {
        this.availableTopics.push(formattedTopic);
      }
      this.selectedTopics.add(formattedTopic);
      this.customTopic = '';
    }
  }

  closePreferences() {
    this.showPreferencesModal = false;
  }

  savePreferences() {
    this.isSavingPreferences = true;
    const preferredTopicsStr = Array.from(this.selectedTopics).join(', ');
    this.authService.updatePreferences(preferredTopicsStr)
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

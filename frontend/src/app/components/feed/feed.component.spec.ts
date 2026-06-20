import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeedComponent } from './feed.component';
import { AuthService } from '../../services/auth.service';
import { FeedService } from '../../services/feed.service';
import { AnalysisService } from '../../services/analysis.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ActivatedRoute } from '@angular/router';
import { LiveNewsArticle } from '../../models/live-news-article';
import { AnalysisResult } from '../../models/analysis-result';

describe('FeedComponent', () => {
  let component: FeedComponent;
  let fixture: ComponentFixture<FeedComponent>;
  let authServiceSpy: any;
  let feedServiceSpy: any;
  let analysisServiceSpy: any;
  let routerSpy: any;

  const mockArticles: LiveNewsArticle[] = [
    { title: 'Article 1', description: 'Desc 1', url: 'http://example.com/1', imageUrl: '', sourceName: 'Source 1', publishedAt: '2023-01-01' }
  ];

  beforeEach(async () => {
    authServiceSpy = { 
        logout: vi.fn(), 
        getPreferences: vi.fn().mockReturnValue(of({ topics: 'AI' })),
        updatePreferences: vi.fn().mockReturnValue(of({}))
    };
    feedServiceSpy = { 
        getLiveNews: vi.fn().mockReturnValue(of(mockArticles)) 
    };
    analysisServiceSpy = { 
        analyze: vi.fn() 
    };
    routerSpy = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [FeedComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: FeedService, useValue: feedServiceSpy },
        { provide: AnalysisService, useValue: analysisServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FeedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component and fetch news on init', () => {
    expect(component).toBeTruthy();
    expect(feedServiceSpy.getLiveNews).toHaveBeenCalled();
    expect(component.articles).toEqual(mockArticles);
  });

  it('should load preferences on init', () => {
    expect(authServiceSpy.getPreferences).toHaveBeenCalled();
    expect(component.selectedTopics.has('AI')).toBe(true);
  });

  it('should open and close preferences modal', () => {
    component.openPreferences();
    expect(component.showPreferencesModal).toBe(true);

    component.closePreferences();
    expect(component.showPreferencesModal).toBe(false);
  });

  it('should save preferences and refresh feed', () => {
    component.selectedTopics.clear();
    component.selectedTopics.add('Space');
    component.savePreferences();

    expect(authServiceSpy.updatePreferences).toHaveBeenCalledWith('Space');
    expect(component.isSavingPreferences).toBe(false);
    expect(component.showPreferencesModal).toBe(false);
    
    // fetchNews is called again inside savePreferences
    expect(feedServiceSpy.getLiveNews).toHaveBeenCalledTimes(2);
  });

  it('should analyze article and display result', () => {
    const url = 'http://example.com/1';
    const mockResult: AnalysisResult = {
        bias_score: 5,
        bias_label: 'Center-Right',
        confidence: 0.9,
        tone: 'Analytical',
        key_indicators: [],
        summary: 'Test summary',
        model_used: 'model',
        topics: ['AI'],
        relevance_score: 80
    };

    analysisServiceSpy.analyze.mockReturnValue(of(mockResult));

    component.analyzeArticle(url);

    expect(analysisServiceSpy.analyze).toHaveBeenCalledWith(url);
    expect(component.analyzingUrls.has(url)).toBe(false);
    expect(component.analysisResults.get(url)).toEqual(mockResult);
  });
});

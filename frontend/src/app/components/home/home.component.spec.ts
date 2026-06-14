import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { AuthService } from '../../services/auth.service';
import { AnalysisService } from '../../services/analysis.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AnalysisResult } from '../../models/analysis-result';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let authServiceSpy: any;
  let analysisServiceSpy: any;
  let routerSpy: any;

  const mockAnalysisResult: AnalysisResult = {
    bias_score: 2.5,
    bias_label: 'Center-Right',
    confidence: 0.85,
    tone: 'Analytical',
    key_indicators: ['Indicator 1'],
    summary: 'Mock summary',
    model_used: 'mock-model',
    topics: ['Topic 1'],
    relevance_score: 90
  };

  beforeEach(async () => {
    authServiceSpy = { logout: vi.fn() };
    analysisServiceSpy = { analyze: vi.fn() };
    routerSpy = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: AnalysisService, useValue: analysisServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should validate input correctly (must be > 10 chars)', () => {
    component.inputText = 'short';
    expect(component.inputIsValid).toBe(false);

    component.inputText = 'This is a sufficiently long text for analysis.';
    expect(component.inputIsValid).toBe(true);
  });

  it('should detect URLs correctly', () => {
    component.inputText = 'just some plain text';
    expect(component.isUrl).toBe(false);

    component.inputText = 'https://www.example.com/article/123';
    expect(component.isUrl).toBe(true);
  });

  it('should call analysisService and display results on success', () => {
    component.inputText = 'This is a long enough text to be valid.';
    
    // Mock successful response
    analysisServiceSpy.analyze.mockReturnValue(of(mockAnalysisResult));

    component.analyze();

    expect(component.isLoading).toBe(false);
    expect(analysisServiceSpy.analyze).toHaveBeenCalledWith(component.inputText);
    expect(component.result).toEqual(mockAnalysisResult);
    expect(component.error).toBeNull();
  });

  it('should handle analysis errors properly', () => {
    component.inputText = 'This is a long enough text to be valid.';
    
    // Mock error response
    analysisServiceSpy.analyze.mockReturnValue(throwError(() => ({ error: { message: 'API failed' } })));

    component.analyze();

    expect(component.isLoading).toBe(false);
    expect(analysisServiceSpy.analyze).toHaveBeenCalledWith(component.inputText);
    expect(component.result).toBeNull();
    expect(component.error).toBe('API failed');
  });

  it('should reset state when reset() is called', () => {
    component.inputText = 'Some text';
    component.result = mockAnalysisResult;
    component.error = 'Some error';

    component.reset();

    expect(component.inputText).toBe('');
    expect(component.result).toBeNull();
    expect(component.error).toBeNull();
  });

  it('should call authService.logout when logout() is clicked', () => {
    component.logout();
    expect(authServiceSpy.logout).toHaveBeenCalled();
  });
});

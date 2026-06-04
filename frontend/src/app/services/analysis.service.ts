import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../environment';
import { AnalysisResult } from '../models/analysis-result';

@Injectable({ providedIn: 'root' })
export class AnalysisService {
  private readonly apiUrl = `${environment.apiBaseUrl}/analysis`;

  constructor(private http: HttpClient) {}

  /**
   * POST /api/analysis/analyze
   * Accepts either a URL to scrape or raw article text.
   */
  analyze(input: string): Observable<AnalysisResult> {
    const payload = this.isUrl(input)
      ? { url: input.trim() }
      : { text: input.trim() };

    return this.http.post<AnalysisResult>(`${this.apiUrl}/analyze`, payload);
  }

  /** Simple URL detection — matches the logic previously in HomeComponent. */
  private isUrl(value: string): boolean {
    try {
      new URL(value.trim());
      return true;
    } catch {
      return false;
    }
  }
}

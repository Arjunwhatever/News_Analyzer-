import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../environment';
import { AnalysisResult } from '../models/analysis-result';

export interface SourceBiasStat {
  sourceName: string;
  averageBias: number;
  articleCount: number;
  description?: string;
}

@Injectable({ providedIn: 'root' })
export class AnalysisService {
  private readonly apiUrl = `${environment.apiBaseUrl}/analysis`;

  constructor(private http: HttpClient) {}

  // This takes whatever the user typed in (a URL or a block of text)
  // and smartly packages it up to send to our backend analysis engine.
  analyze(input: string): Observable<AnalysisResult> {
    const payload = this.isUrl(input)
      ? { url: input.trim() }
      : { text: input.trim() };

    return this.http.post<AnalysisResult>(`${this.apiUrl}/analyze`, payload);
  }

  // Fetch aggregated source bias statistics from the database
  getSourceBiasStats(): Observable<SourceBiasStat[]> {
    return this.http.get<SourceBiasStat[]>(`${this.apiUrl}/sources`);
  }

  // A quick helper to figure out if the user pasted a link or an entire article.
  private isUrl(value: string): boolean {
    try {
      new URL(value.trim());
      return true;
    } catch {
      return false;
    }
  }
}

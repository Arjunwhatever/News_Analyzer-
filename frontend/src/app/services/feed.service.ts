import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LiveNewsArticle } from '../models/live-news-article';

@Injectable({
  providedIn: 'root'
})
export class FeedService {
  private http = inject(HttpClient);

  getLiveNews(weeks: number = 1, category?: string): Observable<LiveNewsArticle[]> {
    let params = new HttpParams().set('weeks', weeks.toString());
    if (category && category !== 'Discover') {
      params = params.set('category', category);
    }
    return this.http.get<LiveNewsArticle[]>('/api/Feed/live', { params });
  }
}

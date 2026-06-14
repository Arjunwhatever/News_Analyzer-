import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LiveNewsArticle } from '../models/live-news-article';

@Injectable({
  providedIn: 'root'
})
export class FeedService {
  private http = inject(HttpClient);

  // Calls our backend which acts as a secure proxy to NewsAPI
  getLiveNews(): Observable<LiveNewsArticle[]> {
    return this.http.get<LiveNewsArticle[]>('/api/Feed/live');
  }
}

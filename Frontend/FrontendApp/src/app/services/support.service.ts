import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SupportMessage } from '../model/message';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SupportService {

  private apiUrl = `${environment.apiBaseUrl}/support`;

  constructor(private http: HttpClient) {}

  sendSupportMessage(data: SupportMessage): Observable<any> {
    return this.http.post(`${this.apiUrl}/contact`, data);
  }
}
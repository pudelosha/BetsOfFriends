import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PrivateMessage } from '../model/message';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PrivateMessageService {

  private apiUrl = `${environment.apiBaseUrl}/private-messages`;

  constructor(private http: HttpClient) { }

  getConversation(withUserId: string): Observable<PrivateMessage[]> {
    return this.http.get<PrivateMessage[]>(`${this.apiUrl}/conversation/${withUserId}`);
  }

  sendMessage(toUserId: string, content: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/send`, { toUserId, content });
  }
}

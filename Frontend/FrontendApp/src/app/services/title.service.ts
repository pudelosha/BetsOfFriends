import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TitleService {
  private titleSubject = new BehaviorSubject<string>('APP.TITLE');
  title$ = this.titleSubject.asObservable();

  setTitle(title: string) {
    this.titleSubject.next(title);
  }

  resetTitle() {
    this.titleSubject.next('APP.TITLE');
  }
}

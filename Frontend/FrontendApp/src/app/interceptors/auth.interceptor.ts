import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    //console.log('AuthInterceptor: Intercepting request:', req.url);

    const token = localStorage.getItem('authToken'); // Ensure token is stored correctly

    if (token) {
      //console.log('AuthInterceptor: Adding Authorization header');
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    } else {
      console.warn('AuthInterceptor: No auth token found');
    }

    return next.handle(req);
  }
}

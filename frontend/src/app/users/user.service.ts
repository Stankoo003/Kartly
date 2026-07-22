import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppUser, PagedResult, UserQuery } from './user.models';

/** Typed client for the admin /api/admin/users endpoints. Token is added by authInterceptor. */
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/admin/users';

  list(query: UserQuery = {}): Observable<PagedResult<AppUser>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<PagedResult<AppUser>>(this.baseUrl, { params });
  }

  get(id: string): Observable<AppUser> {
    return this.http.get<AppUser>(`${this.baseUrl}/${id}`);
  }

  changeRole(id: string, role: string): Observable<AppUser> {
    return this.http.put<AppUser>(`${this.baseUrl}/${id}/role`, { role });
  }

  setActive(id: string, isActive: boolean): Observable<AppUser> {
    return this.http.put<AppUser>(`${this.baseUrl}/${id}/active`, { isActive });
  }
}

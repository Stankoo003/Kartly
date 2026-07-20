import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface AuthResult {
  token: string;
  email: string;
  role: 'Admin' | 'Customer';
  expiresAt: string;
}

interface Credentials {
  email: string;
  password: string;
}

const STORAGE_KEY = 'kartly.auth';

/**
 * Holds the JWT and derived auth state. The token is persisted to
 * localStorage so a page refresh keeps the user signed in until it expires.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<AuthResult | null>(this.restore());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly role = computed(() => this.session()?.role ?? null);
  readonly isAdmin = computed(() => this.role() === 'Admin');
  readonly email = computed(() => this.session()?.email ?? null);

  login(credentials: Credentials): Observable<AuthResult> {
    return this.http
      .post<AuthResult>('/api/auth/login', credentials)
      .pipe(tap(result => this.persist(result)));
  }

  register(credentials: Credentials): Observable<AuthResult> {
    return this.http
      .post<AuthResult>('/api/auth/register', credentials)
      .pipe(tap(result => this.persist(result)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }

  /** The raw bearer token, or null. Consumed by the HTTP interceptor. */
  token(): string | null {
    const current = this.session();
    if (!current) return null;

    // Drop an expired token so we don't attach it to requests.
    if (new Date(current.expiresAt).getTime() <= Date.now()) {
      this.logout();
      return null;
    }
    return current.token;
  }

  private persist(result: AuthResult): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
    this.session.set(result);
  }

  private restore(): AuthResult | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthResult;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}

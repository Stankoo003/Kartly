import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateProductRequest,
  PagedResult,
  Product,
  ProductQuery,
  UpdateProductRequest,
} from './product.models';

/** Typed client for the /api/products endpoints. Token is added by authInterceptor. */
@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  list(query: ProductQuery = {}): Observable<PagedResult<Product>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<PagedResult<Product>>(this.baseUrl, { params });
  }

  get(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  /** Fetch a product by its slug — powers the deep-linkable detail page. 404 when missing/inactive. */
  getBySlug(slug: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/slug/${encodeURIComponent(slug)}`);
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }

  update(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Uploads an image (multipart) and resolves to its stored public URL. */
  uploadImage(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string }>(`${this.baseUrl}/images`, form);
  }
}

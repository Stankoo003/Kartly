import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../products/product.models';
import { Order, OrderQuery, OrderStatus, OrderSummary, PlaceOrderRequest } from './order.models';

/** Client for the order endpoints. Placement/read-back are public; admin list/status need a token. */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  /** Places an order (public). The server re-validates prices/stock and may reject with a 400. */
  place(request: PlaceOrderRequest): Observable<Order> {
    return this.http.post<Order>('/api/orders', request);
  }

  /** Reads one order back for the confirmation page (public — id is an unguessable GUID). */
  get(id: string): Observable<Order> {
    return this.http.get<Order>(`/api/orders/${id}`);
  }

  // --- admin ---

  list(query: OrderQuery = {}): Observable<PagedResult<OrderSummary>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') params = params.set(key, String(value));
    }
    return this.http.get<PagedResult<OrderSummary>>('/api/admin/orders', { params });
  }

  getAdmin(id: string): Observable<Order> {
    return this.http.get<Order>(`/api/admin/orders/${id}`);
  }

  setStatus(id: string, status: OrderStatus): Observable<Order> {
    return this.http.put<Order>(`/api/admin/orders/${id}/status`, { status });
  }
}

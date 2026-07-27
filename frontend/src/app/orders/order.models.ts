/** Frontend mirror of the backend order DTOs (backend Kartly.Application/Orders/OrderDtos.cs). */

export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Cancelled';

export interface OrderLineRequest {
  productId: string;
  quantity: number;
  /** The unit price the client expects to pay — re-validated server-side. */
  unitPrice: number;
}

export interface PlaceOrderRequest {
  contactEmail: string;
  contactPhone: string;
  shipFirstName: string;
  shipLastName: string;
  shipAddress: string;
  shipCity: string;
  shipZip: string;
  shipCountry: string;
  items: OrderLineRequest[];
}

export interface OrderLine {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  contactEmail: string;
  contactPhone: string;
  shipFirstName: string;
  shipLastName: string;
  shipAddress: string;
  shipCity: string;
  shipZip: string;
  shipCountry: string;
  status: OrderStatus;
  total: number;
  /**
   * ISO 4217 code `total` and every line amount are in, snapshotted at placement. Render an order
   * in *this* currency, never in the site's current display currency — the amounts were never
   * converted, so relabelling them would restate what the order was worth.
   */
  currency: string;
  createdAt: string;
  lines: OrderLine[];
}

export interface OrderSummary {
  id: string;
  contactEmail: string;
  status: OrderStatus;
  total: number;
  /** See Order.currency. */
  currency: string;
  itemCount: number;
  createdAt: string;
}

export interface OrderQuery {
  status?: OrderStatus | '';
  page?: number;
  pageSize?: number;
}

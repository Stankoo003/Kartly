/** A single line in the cart. Snapshots the product at add-time (there is no cart backend). */
export interface CartItem {
  productId: string;
  slug: string;
  name: string;
  imageUrl: string | null;
  /**
   * Unit price in **base-currency** major units, exactly as the API returned it. Deliberately not
   * converted and deliberately carrying no currency of its own: the server re-validates this
   * against Product.Price at checkout, which is also base. Store a converted figure here and
   * checkout starts rejecting every order with "the price has changed". Display conversion
   * happens at render time, via MoneyPipe.
   */
  price: number;
  /** Available stock at add-time — quantity is capped at this. */
  stock: number;
  qty: number;
}

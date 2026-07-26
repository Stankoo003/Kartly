/** A single line in the cart. Snapshots the product at add-time (there is no cart backend). */
export interface CartItem {
  productId: string;
  slug: string;
  name: string;
  imageUrl: string | null;
  /** Unit price in major units, as returned by the API. */
  price: number;
  /** Available stock at add-time — quantity is capped at this. */
  stock: number;
  qty: number;
}

import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProductService } from '../products/product.service';
import {
  CreateProductRequest,
  PRODUCT_CATEGORIES,
  Product,
  UpdateProductRequest,
} from '../products/product.models';
import { UiButton } from '../ui/ui-button';
import { UiDialog } from '../ui/ui-dialog';

type DialogMode = 'create' | 'edit' | 'delete' | null;
type StatusFilter = 'active' | 'inactive' | 'all';

/** Client-side image rules — mirror the server (ImageUploadRules). */
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

interface FormModel {
  name: string;
  category: string;
  price: number | null;
  slug: string;
  sku: string;
  brand: string;
  model: string;
  description: string;
  imageUrl: string | null;
  discountPrice: number | null;
  stockQuantity: number | null;
  warrantyMonths: number | null;
  isFeatured: boolean;
  isActive: boolean;
}

const emptyForm = (): FormModel => ({
  name: '',
  category: '',
  price: null,
  slug: '',
  sku: '',
  brand: '',
  model: '',
  description: '',
  imageUrl: null,
  discountPrice: null,
  stockQuantity: 0,
  warrantyMonths: null,
  isFeatured: false,
  isActive: true,
});

/** Admin-only products screen: paginated list + create/edit/delete against the live API. */
@Component({
  selector: 'app-admin-products',
  imports: [DecimalPipe, FormsModule, UiButton, UiDialog],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.scss',
})
export class AdminProducts {
  private readonly api = inject(ProductService);

  protected readonly categories = PRODUCT_CATEGORIES;

  protected readonly products = signal<Product[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly status = signal<StatusFilter>('active');

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly saving = signal(false);
  protected readonly uploading = signal(false);

  protected readonly dialogMode = signal<DialogMode>(null);
  protected readonly formError = signal('');
  protected readonly fieldErrors = signal<Record<string, string>>({});
  protected form: FormModel = emptyForm();
  private editingId: string | null = null;
  protected deleting: Product | null = null;

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize)),
  );

  constructor() {
    this.load();
  }

  /** Maps the status filter to the API's isActive param (undefined = all). */
  private isActiveParam(): boolean | undefined {
    switch (this.status()) {
      case 'active': return true;
      case 'inactive': return false;
      default: return undefined;
    }
  }

  protected setStatus(value: StatusFilter): void {
    if (value === this.status()) return;
    this.status.set(value);
    this.page.set(1);
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api
      .list({
        page: this.page(),
        pageSize: this.pageSize,
        sortBy: 'CreatedAt',
        sortDescending: true,
        isActive: this.isActiveParam(),
      })
      .subscribe({
        next: result => {
          this.products.set(result.items);
          this.total.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load products. Is the API running and are you signed in as Admin?');
          this.loading.set(false);
        },
      });
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    this.load();
  }

  // --- dialogs ---

  protected openCreate(): void {
    this.form = emptyForm();
    this.editingId = null;
    this.resetFormFeedback();
    this.dialogMode.set('create');
  }

  protected openEdit(p: Product): void {
    this.form = {
      name: p.name,
      category: p.category,
      price: p.price,
      slug: p.slug,
      sku: p.sku,
      brand: p.brand ?? '',
      model: p.model ?? '',
      description: p.description ?? '',
      imageUrl: p.imageUrl,
      discountPrice: p.discountPrice,
      stockQuantity: p.stockQuantity,
      warrantyMonths: p.warrantyMonths,
      isFeatured: p.isFeatured,
      isActive: p.isActive,
    };
    this.editingId = p.id;
    this.resetFormFeedback();
    this.dialogMode.set('edit');
  }

  private resetFormFeedback(): void {
    this.formError.set('');
    this.fieldErrors.set({});
    this.uploading.set(false);
  }

  // --- image upload ---

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      this.formError.set('Unsupported image type. Allowed types: JPEG, PNG, WebP.');
      input.value = '';
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      this.formError.set('Image exceeds the 5 MB limit.');
      input.value = '';
      return;
    }

    this.uploading.set(true);
    this.formError.set('');
    this.api.uploadImage(file).subscribe({
      next: ({ url }) => {
        this.form.imageUrl = url;
        this.uploading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.formError.set(this.describeError(err));
        this.uploading.set(false);
      },
    });
    input.value = '';
  }

  protected removeImage(): void {
    this.form.imageUrl = null;
  }

  protected openDelete(p: Product): void {
    this.deleting = p;
    this.dialogMode.set('delete');
  }

  protected closeDialog(): void {
    if (this.saving()) return;
    this.dialogMode.set(null);
  }

  // --- mutations ---

  /** Client-side validation mirroring the server rules. Returns true when the form is valid. */
  private validateForm(): boolean {
    const f = this.form;
    const errors: Record<string, string> = {};

    if (!f.name.trim()) errors['name'] = 'Name is required.';
    if (!f.category) errors['category'] = 'Category is required.';

    if (f.price === null || Number.isNaN(f.price)) errors['price'] = 'Price is required.';
    else if (f.price < 0) errors['price'] = 'Price cannot be negative.';

    if (f.discountPrice != null && f.discountPrice < 0)
      errors['discountPrice'] = 'Discount price cannot be negative.';
    else if (f.discountPrice != null && f.price != null && f.discountPrice > f.price)
      errors['discountPrice'] = 'Discount price cannot exceed price.';

    if (f.stockQuantity != null && f.stockQuantity < 0)
      errors['stockQuantity'] = 'Stock quantity cannot be negative.';
    if (f.warrantyMonths != null && f.warrantyMonths < 0)
      errors['warrantyMonths'] = 'Warranty months cannot be negative.';

    if (this.editingId) {
      if (!f.slug.trim()) errors['slug'] = 'Slug is required.';
      if (!f.sku.trim()) errors['sku'] = 'SKU is required.';
    }

    this.fieldErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  protected submitForm(): void {
    if (!this.validateForm()) return;

    this.saving.set(true);
    this.formError.set('');

    const done = {
      next: () => {
        this.saving.set(false);
        this.dialogMode.set(null);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.formError.set(this.describeError(err));
      },
    };

    if (this.editingId) {
      this.api.update(this.editingId, this.toUpdateRequest()).subscribe(done);
    } else {
      this.api.create(this.toCreateRequest()).subscribe(done);
    }
  }

  protected confirmDelete(): void {
    if (!this.deleting) return;
    this.saving.set(true);
    this.api.remove(this.deleting.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogMode.set(null);
        this.deleting = null;
        // Deleting the last row on a page — step back so we don't land on an empty page.
        if (this.products().length === 1 && this.page() > 1) this.page.set(this.page() - 1);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.error.set(this.describeError(err));
        this.dialogMode.set(null);
      },
    });
  }

  /** Restores a soft-deleted product by re-activating it (full-replace PUT with isActive=true). */
  protected reactivate(p: Product): void {
    this.api
      .update(p.id, {
        name: p.name,
        slug: p.slug,
        sku: p.sku,
        category: p.category,
        price: p.price,
        brand: p.brand,
        model: p.model,
        description: p.description,
        imageUrl: p.imageUrl,
        discountPrice: p.discountPrice,
        stockQuantity: p.stockQuantity,
        warrantyMonths: p.warrantyMonths,
        isFeatured: p.isFeatured,
        isActive: true,
      })
      .subscribe({
        next: () => this.load(),
        error: (err: HttpErrorResponse) => this.error.set(this.describeError(err)),
      });
  }

  // --- helpers ---

  private toCreateRequest(): CreateProductRequest {
    const f = this.form;
    return {
      name: f.name.trim(),
      category: f.category,
      price: Number(f.price ?? 0),
      slug: f.slug.trim() || undefined,
      sku: f.sku.trim() || undefined,
      brand: f.brand.trim() || null,
      model: f.model.trim() || null,
      description: f.description.trim() || null,
      imageUrl: f.imageUrl,
      discountPrice: f.discountPrice ?? null,
      stockQuantity: Number(f.stockQuantity ?? 0),
      warrantyMonths: f.warrantyMonths ?? null,
      isFeatured: f.isFeatured,
      isActive: f.isActive,
    };
  }

  private toUpdateRequest(): UpdateProductRequest {
    const f = this.form;
    return {
      name: f.name.trim(),
      slug: f.slug.trim(),
      sku: f.sku.trim(),
      category: f.category,
      price: Number(f.price ?? 0),
      brand: f.brand.trim() || null,
      model: f.model.trim() || null,
      description: f.description.trim() || null,
      imageUrl: f.imageUrl,
      discountPrice: f.discountPrice ?? null,
      stockQuantity: Number(f.stockQuantity ?? 0),
      warrantyMonths: f.warrantyMonths ?? null,
      isFeatured: f.isFeatured,
      isActive: f.isActive,
    };
  }

  /** Turn an API error into a readable message: validation 400 fields, 409 conflict, etc. */
  private describeError(err: HttpErrorResponse): string {
    const body = err.error;
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, string[]>).flat();
      if (messages.length) return messages.join(' ');
    }
    if (typeof body?.error === 'string') return body.error;
    if (typeof body?.title === 'string') return body.title;
    return 'Something went wrong. Please try again.';
  }
}

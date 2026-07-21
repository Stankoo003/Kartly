import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProductService } from '../products/product.service';
import {
  CreateProductRequest,
  Product,
  UpdateProductRequest,
} from '../products/product.models';
import { UiButton } from '../ui/ui-button';
import { UiDialog } from '../ui/ui-dialog';

type DialogMode = 'create' | 'edit' | 'delete' | null;

interface FormModel {
  name: string;
  price: number | null;
  slug: string;
  sku: string;
  brand: string;
  model: string;
  description: string;
  discountPrice: number | null;
  stockQuantity: number | null;
  warrantyMonths: number | null;
  isFeatured: boolean;
  isActive: boolean;
}

const emptyForm = (): FormModel => ({
  name: '',
  price: null,
  slug: '',
  sku: '',
  brand: '',
  model: '',
  description: '',
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

  protected readonly products = signal<Product[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 10;

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly saving = signal(false);

  protected readonly dialogMode = signal<DialogMode>(null);
  protected readonly formError = signal('');
  protected form: FormModel = emptyForm();
  private editingId: string | null = null;
  protected deleting: Product | null = null;

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize)),
  );

  constructor() {
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
        isActive: true,
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
    this.formError.set('');
    this.dialogMode.set('create');
  }

  protected openEdit(p: Product): void {
    this.form = {
      name: p.name,
      price: p.price,
      slug: p.slug,
      sku: p.sku,
      brand: p.brand ?? '',
      model: p.model ?? '',
      description: p.description ?? '',
      discountPrice: p.discountPrice,
      stockQuantity: p.stockQuantity,
      warrantyMonths: p.warrantyMonths,
      isFeatured: p.isFeatured,
      isActive: p.isActive,
    };
    this.editingId = p.id;
    this.formError.set('');
    this.dialogMode.set('edit');
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

  protected submitForm(): void {
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

  // --- helpers ---

  private toCreateRequest(): CreateProductRequest {
    const f = this.form;
    return {
      name: f.name.trim(),
      price: Number(f.price ?? 0),
      slug: f.slug.trim() || undefined,
      sku: f.sku.trim() || undefined,
      brand: f.brand.trim() || null,
      model: f.model.trim() || null,
      description: f.description.trim() || null,
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
      price: Number(f.price ?? 0),
      brand: f.brand.trim() || null,
      model: f.model.trim() || null,
      description: f.description.trim() || null,
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

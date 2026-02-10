import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, of, switchMap, tap } from 'rxjs';

import { CatalogApiService } from '../core/catalog-api.service';
import { CategoryDto, CreateProductRequest, ProductDto } from '../core/models';

@Component({
  selector: 'app-product-form-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="page">
      <header class="page__header">
        <div>
          <h2>{{ isEdit() ? 'Edit product' : 'Add product' }}</h2>
          <p class="muted">Required fields are validated client-side and server-side.</p>
        </div>
      </header>

      <div *ngIf="loading()" class="card">Loading…</div>
      <div *ngIf="error()" class="card card--error">{{ error() }}</div>

      <form class="card form" [formGroup]="form" (ngSubmit)="onSubmit()" *ngIf="!loading()">
        <div class="grid">
          <label>
            <span>Name</span>
            <input class="input" formControlName="name" />
            <small class="hint" *ngIf="form.controls.name.touched && form.controls.name.invalid">Name is required</small>
          </label>

          <label>
            <span>SKU</span>
            <input class="input" formControlName="sku" />
            <small class="hint" *ngIf="form.controls.sku.touched && form.controls.sku.invalid">SKU is required</small>
          </label>

          <label>
            <span>Price</span>
            <input class="input" type="number" step="0.01" formControlName="price" />
            <small class="hint" *ngIf="form.controls.price.touched && form.controls.price.invalid">Must be positive</small>
          </label>

          <label>
            <span>Quantity</span>
            <input class="input" type="number" formControlName="quantity" />
            <small class="hint" *ngIf="form.controls.quantity.touched && form.controls.quantity.invalid">Must be 0 or more</small>
          </label>

          <label class="col-span">
            <span>Category</span>
            <select class="select" formControlName="categoryId">
              <option value="">Select category</option>
              <option *ngFor="let c of categories()" [value]="c.id">{{ c.name }}</option>
            </select>
            <small class="hint" *ngIf="form.controls.categoryId.touched && form.controls.categoryId.invalid">Category is required</small>
          </label>

          <label class="col-span">
            <span>Description</span>
            <textarea class="input" rows="4" formControlName="description"></textarea>
          </label>
        </div>

        <div class="actions">
          <button class="button" type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Saving…' : 'Save' }}
          </button>
          <button class="button button--ghost" type="button" (click)="cancel()">Cancel</button>
        </div>
      </form>
    </section>
  `,
  styles: [
    `
      .page {
        max-width: 900px;
        margin: 0 auto;
      }
      .page__header {
        margin-bottom: 1rem;
      }
      .card {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        padding: 1rem;
      }
      .card--error {
        border-color: #ef4444;
        color: #b91c1c;
        background: #fef2f2;
        margin-bottom: 1rem;
      }
      .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.75rem;
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }
      .col-span {
        grid-column: 1 / -1;
      }
      .input,
      .select {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.6rem 0.75rem;
        background: white;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 1rem;
      }
      .button {
        border: 1px solid #111827;
        background: #111827;
        color: white;
        padding: 0.5rem 0.75rem;
        border-radius: 0.375rem;
        cursor: pointer;
      }
      .button--ghost {
        background: transparent;
        color: #111827;
      }
      .button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .hint {
        color: #6b7280;
      }
      .muted {
        color: #6b7280;
      }
      @media (max-width: 780px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class ProductFormPage {
  private readonly api = inject(CatalogApiService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly categories = signal<CategoryDto[]>([]);

  protected readonly id = signal<string | null>(null);
  protected readonly isEdit = computed(() => !!this.id());

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    sku: ['', [Validators.required]],
    price: [0, [Validators.required, Validators.min(0.01)]],
    quantity: [0, [Validators.required, Validators.min(0)]],
    categoryId: ['', [Validators.required]],
    description: ['']
  });

  constructor() {
    this.loading.set(true);

    this.api
      .listCategories()
      .pipe(
        catchError(() => of([] as CategoryDto[])),
        tap((items) => this.categories.set(items)),
        switchMap(() => this.route.paramMap),
        tap((params) => this.id.set(params.get('id'))),
        switchMap((params) => {
          const id = params.get('id');
          if (!id) return of(null);
          return this.api.getProduct(id).pipe(
            catchError((err) => {
              this.error.set(err?.message ?? 'Failed to load product');
              return of(null);
            })
          );
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((product) => {
        if (!product) return;
        this.patchFromProduct(product);
      });
  }

  protected cancel(): void {
    this.router.navigateByUrl('/products');
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const value = this.form.getRawValue();
    const request: CreateProductRequest = {
      name: value.name,
      sku: value.sku,
      price: value.price,
      quantity: value.quantity,
      categoryId: value.categoryId,
      description: value.description ? value.description : null
    };

    const op$ = this.id()
      ? this.api.updateProduct(this.id()!, request)
      : this.api.createProduct(request);

    op$
      .pipe(
        catchError((err) => {
          this.error.set(err?.error?.error ?? err?.message ?? 'Failed to save product');
          return of(null);
        }),
        finalize(() => this.saving.set(false))
      )
      .subscribe((saved) => {
        if (!saved) return;
        this.router.navigateByUrl('/products');
      });
  }

  private patchFromProduct(product: ProductDto): void {
    this.form.patchValue({
      name: product.name,
      sku: product.sku,
      price: product.price,
      quantity: product.quantity,
      categoryId: product.categoryId,
      description: product.description ?? ''
    });
  }
}

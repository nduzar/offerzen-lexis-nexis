import { AsyncPipe, CurrencyPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, finalize, map, of, startWith, Subject, switchMap, tap } from 'rxjs';

import { CatalogApiService } from '../core/catalog-api.service';
import { CategoryDto, ProductDto, ProductSearchResponse } from '../core/models';

@Component({
  selector: 'app-product-list-page',
  standalone: true,
  imports: [NgIf, NgFor, AsyncPipe, RouterLink, CurrencyPipe, DatePipe],
  template: `
    <section class="page">
      <header class="page__header">
        <div>
          <h2>Products</h2>
          <p class="muted">Search by name / SKU / description, filter by category.</p>
        </div>

        <a class="button" routerLink="/products/new">Add product</a>
      </header>

      <div class="toolbar">
        <input
          class="input"
          type="text"
          [value]="search()"
          (input)="onSearch(($any($event.target)).value)"
          placeholder="Search (fuzzy supported)…"
        />

        <select class="select" [value]="categoryId() ?? ''" (change)="onCategory(($any($event.target)).value)">
          <option value="">All categories</option>
          <option *ngFor="let c of categories()" [value]="c.id">{{ c.name }}</option>
        </select>
      </div>

      <div *ngIf="loading()" class="card">Loading…</div>
      <div *ngIf="error()" class="card card--error">{{ error() }}</div>

      <div class="card" *ngIf="(vm$ | async) as vm">
        <div class="table-wrap">
          <div class="table-meta">
            <div class="muted">Total: {{ vm.total }}</div>
            <div class="pager">
              <button class="button button--ghost" (click)="prevPage()" [disabled]="page() <= 1">Prev</button>
              <span class="muted">Page {{ page() }}</span>
              <button
                class="button button--ghost"
                (click)="nextPage(vm.total)"
                [disabled]="page() * pageSize() >= vm.total"
              >
                Next
              </button>
            </div>
          </div>

          <table class="table" *ngIf="!error()">
            <thead>
              <tr>
                <th>Name</th>
                <th>SKU</th>
                <th>Price</th>
                <th>Qty</th>
                <th>Updated</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of vm.items">
                <td>{{ p.name }}</td>
                <td class="mono">{{ p.sku }}</td>
                <td>{{ p.price | currency }}</td>
                <td>{{ p.quantity }}</td>
                <td>{{ p.updatedAt | date: 'medium' }}</td>
                <td class="actions">
                  <a class="button button--ghost" [routerLink]="['/products', p.id]">Edit</a>
                  <button class="button button--danger" (click)="confirmDelete(p)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .page {
        max-width: 1100px;
        margin: 0 auto;
      }
      .page__header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .toolbar {
        display: grid;
        grid-template-columns: 1fr 260px;
        gap: 0.75rem;
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
      }
      .table-wrap {
        overflow-x: auto;
      }
      .table {
        width: 100%;
        border-collapse: collapse;
      }
      .table th,
      .table td {
        padding: 0.75rem;
        border-bottom: 1px solid #e5e7eb;
        text-align: left;
      }
      .table-meta {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
      }
      .pager {
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.5rem;
        white-space: nowrap;
      }
      .input,
      .select {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.6rem 0.75rem;
        background: white;
      }
      .button {
        display: inline-block;
        border: 1px solid #111827;
        background: #111827;
        color: white;
        padding: 0.5rem 0.75rem;
        border-radius: 0.375rem;
        cursor: pointer;
        text-decoration: none;
        font-size: 0.9rem;
      }
      .button--ghost {
        background: transparent;
        color: #111827;
      }
      .button--danger {
        border-color: #ef4444;
        background: #ef4444;
        color: #fff;
      }
      .button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .muted {
        color: #6b7280;
      }
      .mono {
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono',
          'Courier New', monospace;
      }
    `
  ]
})
export class ProductListPage {
  private readonly api = inject(CatalogApiService);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly search = signal('');
  protected readonly categoryId = signal<string | null>(null);

  private readonly refresh$ = new Subject<void>();

  protected readonly categories = signal<CategoryDto[]>([]);

  protected readonly vm$ = this.refresh$.pipe(
    startWith(void 0),
    debounceTime(10),
    tap(() => {
      this.loading.set(true);
      this.error.set(null);
    }),
    switchMap(() =>
      this.api
        .listProducts({
          page: this.page(),
          pageSize: this.pageSize(),
          categoryId: this.categoryId(),
          search: this.search() || null
        })
        .pipe(
          catchError((err) => {
            this.error.set(err?.message ?? 'Failed to load products');
            return of({ items: [], total: 0, page: 1, pageSize: this.pageSize() } satisfies ProductSearchResponse);
          }),
          finalize(() => this.loading.set(false))
        )
    ),
    map((resp) => ({ items: resp.items, total: resp.total }))
  );

  constructor() {
    this.loadCategories();

    effect(() => {
      this.page();
      this.pageSize();
      this.search();
      this.categoryId();
      this.refresh();
    });
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  protected onCategory(value: string): void {
    this.categoryId.set(value ? value : null);
    this.page.set(1);
  }

  protected prevPage(): void {
    this.page.set(Math.max(1, this.page() - 1));
  }

  protected nextPage(total: number): void {
    if (this.page() * this.pageSize() >= total) {
      return;
    }

    this.page.set(this.page() + 1);
  }

  protected confirmDelete(p: ProductDto): void {
    const ok = window.confirm(`Delete "${p.name}"?`);
    if (!ok) return;

    this.loading.set(true);
    this.api
      .deleteProduct(p.id)
      .pipe(
        catchError((err) => {
          this.error.set(err?.message ?? 'Failed to delete product');
          return of(void 0);
        })
      )
      .subscribe(() => {
        this.loading.set(false);
        this.refresh();
      });
  }

  private loadCategories(): void {
    this.api
      .listCategories()
      .pipe(
        catchError(() => of([] as CategoryDto[]))
      )
      .subscribe((items) => this.categories.set(items));
  }

  private refresh(): void {
    this.refresh$.next();
  }
}

import { CommonModule } from '@angular/common';
import { Component, inject, Input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';

import { CatalogApiService } from '../core/catalog-api.service';
import { CategoryDto, CategoryTreeNodeDto, CreateCategoryRequest } from '../core/models';

@Component({
  selector: 'app-category-children',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ul class="tree" *ngIf="nodes.length">
      <li *ngFor="let n of nodes">
        <div class="node">{{ n.name }}</div>
        <app-category-children [nodes]="n.children" />
      </li>
    </ul>
  `,
  styles: [
    `
      .tree {
        margin: 0;
        padding-left: 1rem;
      }
      .node {
        font-weight: 500;
      }
    `
  ]
})
export class CategoryChildrenComponent {
  @Input({ required: true }) nodes: CategoryTreeNodeDto[] = [];
}

@Component({
  selector: 'app-categories-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CategoryChildrenComponent],
  template: `
    <section class="page">
      <header class="page__header">
        <div>
          <h2>Categories</h2>
          <p class="muted">Flat list + hierarchical tree.</p>
        </div>
      </header>

      <div class="grid">
        <div class="card">
          <h3>Create category</h3>

          <div *ngIf="error()" class="error">{{ error() }}</div>

          <form [formGroup]="form" (ngSubmit)="onCreate()" class="form">
            <label>
              <span>Name</span>
              <input class="input" formControlName="name" />
            </label>

            <label>
              <span>Description</span>
              <input class="input" formControlName="description" />
            </label>

            <label>
              <span>Parent category</span>
              <select class="select" formControlName="parentCategoryId">
                <option value="">(root)</option>
                <option *ngFor="let c of categories()" [value]="c.id">{{ c.name }}</option>
              </select>
            </label>

            <button class="button" type="submit" [disabled]="form.invalid || saving()">
              {{ saving() ? 'Creating…' : 'Create' }}
            </button>
          </form>
        </div>

        <div class="card">
          <h3>Category tree</h3>

          <div *ngIf="loading()">Loading…</div>
          <div *ngIf="!loading()">
            <ng-container *ngIf="tree().length; else empty">
              <ul class="tree">
                <ng-container *ngFor="let node of tree()">
                  <li>
                    <div class="node">{{ node.name }}</div>
                    <app-category-children [nodes]="node.children" />
                  </li>
                </ng-container>
              </ul>
            </ng-container>

            <ng-template #empty>
              <div class="muted">No categories yet.</div>
            </ng-template>
          </div>
        </div>

        <div class="card">
          <h3>Flat list</h3>
          <div *ngIf="loading()">Loading…</div>
          <ul *ngIf="!loading()" class="list">
            <li *ngFor="let c of categories()">
              <span>{{ c.name }}</span>
              <span class="muted">{{ c.parentCategoryId ? 'child' : 'root' }}</span>
            </li>
          </ul>
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
        margin-bottom: 1rem;
      }
      .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }
      .card {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        padding: 1rem;
      }
      .form {
        display: grid;
        gap: 0.75rem;
      }
      label {
        display: grid;
        gap: 0.25rem;
      }
      .input,
      .select {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.6rem 0.75rem;
        background: white;
      }
      .button {
        border: 1px solid #111827;
        background: #111827;
        color: white;
        padding: 0.5rem 0.75rem;
        border-radius: 0.375rem;
        cursor: pointer;
        width: fit-content;
      }
      .button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .muted {
        color: #6b7280;
      }
      .error {
        border: 1px solid #ef4444;
        background: #fef2f2;
        color: #b91c1c;
        padding: 0.5rem 0.75rem;
        border-radius: 0.5rem;
        margin-bottom: 0.75rem;
      }
      .tree {
        margin: 0;
        padding-left: 1rem;
      }
      .node {
        font-weight: 600;
      }
      .list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: 0.5rem;
      }
      .list li {
        display: flex;
        justify-content: space-between;
        border-bottom: 1px solid #e5e7eb;
        padding: 0.5rem 0;
      }
      @media (max-width: 980px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class CategoriesPage {
  private readonly api = inject(CatalogApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly tree = signal<CategoryTreeNodeDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    description: [''],
    parentCategoryId: ['']
  });

  constructor() {
    this.reload();
  }

  protected onCreate(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const value = this.form.getRawValue();

    const request: CreateCategoryRequest = {
      name: value.name,
      description: value.description ? value.description : null,
      parentCategoryId: value.parentCategoryId ? value.parentCategoryId : null
    };

    this.api
      .createCategory(request)
      .pipe(
        catchError((err) => {
          this.error.set(err?.message ?? 'Failed to create category');
          return of(null);
        }),
        finalize(() => this.saving.set(false))
      )
      .subscribe((created) => {
        if (!created) return;
        this.form.reset({ name: '', description: '', parentCategoryId: '' });
        this.reload();
      });
  }

  private reload(): void {
    this.loading.set(true);

    this.api
      .listCategories()
      .pipe(
        catchError(() => of([] as CategoryDto[])),
        finalize(() => this.loading.set(false))
      )
      .subscribe((items) => this.categories.set(items));

    this.api
      .getCategoryTree()
      .pipe(catchError(() => of([] as CategoryTreeNodeDto[])))
      .subscribe((tree) => this.tree.set(tree));
  }
}

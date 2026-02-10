import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_BASE_URL } from './api.config';
import {
  CategoryDto,
  CategoryTreeNodeDto,
  CreateCategoryRequest,
  CreateProductRequest,
  ProductDto,
  ProductSearchResponse,
  UpdateProductRequest
} from './models';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);

  listProducts(params: {
    page: number;
    pageSize: number;
    categoryId?: string | null;
    name?: string | null;
    search?: string | null;
  }): Observable<ProductSearchResponse> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.name) httpParams = httpParams.set('name', params.name);
    if (params.search) httpParams = httpParams.set('search', params.search);

    return this.http.get<ProductSearchResponse>(`${API_BASE_URL}/api/products`, { params: httpParams });
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${API_BASE_URL}/api/products/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>(`${API_BASE_URL}/api/products`, request);
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<ProductDto> {
    return this.http.put<ProductDto>(`${API_BASE_URL}/api/products/${id}`, request);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/products/${id}`);
  }

  listCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${API_BASE_URL}/api/categories`);
  }

  getCategoryTree(): Observable<CategoryTreeNodeDto[]> {
    return this.http.get<CategoryTreeNodeDto[]>(`${API_BASE_URL}/api/categories/tree`);
  }

  createCategory(request: CreateCategoryRequest): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(`${API_BASE_URL}/api/categories`, request);
  }
}

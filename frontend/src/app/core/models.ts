export interface ProductDto {
  id: string;
  name: string;
  description: string | null;
  sku: string;
  price: number;
  quantity: number;
  categoryId: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProductSearchResponse {
  items: ProductDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  sku: string;
  price: number;
  quantity: number;
  categoryId: string;
}

export type UpdateProductRequest = CreateProductRequest;

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  description: string | null;
  parentCategoryId: string | null;
}

export interface CategoryTreeNodeDto {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  children: CategoryTreeNodeDto[];
}

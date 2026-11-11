// Respuesta paginada genérica del back
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// Parámetros de paginación
export interface PageParams {
  page?: number;
  pageSize?: number;
}

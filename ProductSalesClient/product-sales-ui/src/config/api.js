export const API_BASE_URL = process.env.VUE_APP_API_BASE_URL || 'https://localhost:7040/api'

export const API_ENDPOINTS = {
  // Product CRUD
  PRODUCTS_SEARCH: `${API_BASE_URL}/products/search`,
  PRODUCTS_MERGE: `${API_BASE_URL}/products/merge`,
  PRODUCTS: `${API_BASE_URL}/products`,
  PRODUCTS_CHECK_SKU: `${API_BASE_URL}/products/check-sku`,
  PRODUCTS_RESERVE_SKU: `${API_BASE_URL}/products/reserve-sku`,
  
  // Export operations
  PRODUCTS_EXPORT_TABLE: `${API_BASE_URL}/products/export`,
  PRODUCTS_EXPORT_TREE: `${API_BASE_URL}/products/export`,
  PRODUCTS_EXPORT_IMPORT: `${API_BASE_URL}/products/export-import`,
  
  // Import operations
  PRODUCTS_UPLOAD: `${API_BASE_URL}/products/upload-file`,
  PRODUCTS_IMPORT_STAGING: `${API_BASE_URL}/products/import-staging`,
  PRODUCTS_UPDATE_STAGING: `${API_BASE_URL}/products/update-staging`,
  PRODUCTS_DELETE_STAGING: `${API_BASE_URL}/products/delete-staging`,
  PRODUCTS_PROCESS_IMPORT: `${API_BASE_URL}/products/process-import`,
  PRODUCTS_CLEAR_IMPORT: `${API_BASE_URL}/products/clear-import`,
  PRODUCTS_DOWNLOAD_TEMPLATE: `${API_BASE_URL}/products/download-template`,
  
  // Images
  IMAGES_UPLOAD: `${API_BASE_URL}/images/upload`,
  IMAGES_DELETE: `${API_BASE_URL}/images`
}
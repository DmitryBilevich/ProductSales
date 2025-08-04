import axios from 'axios'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import Galleria from 'primevue/galleria'
import Dialog from 'primevue/dialog'
import Card from 'primevue/card'
import SelectButton from 'primevue/selectbutton'
import Checkbox from 'primevue/checkbox'
import Dropdown from 'primevue/dropdown'
import MultiSelect from 'primevue/multiselect'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import ProductImageManager from '../ProductImageManager.vue'
import TabView from 'primevue/tabview'
import TabPanel from 'primevue/tabpanel'
import ImageUploader from '../ImageUploader'

export default {
  name: 'ProductList',
  components: {
    DataTable,
    Column,
    InputText,
    InputNumber,
    Button,
    Galleria,
    Dialog,
    Card,
    SelectButton,
    Checkbox,
    Dropdown,
    MultiSelect,
    Tag,
    Textarea,
    TabView,
    TabPanel,
    ProductImageManager,
    ImageUploader
  },
data() {
  return {
    // View Management
    currentView: 'table',
    showAdvancedFilters: false,
    
    // Filters
    filters: {
      name: '',
      category: '',
      sku: '',
      priceMin: null,
      priceMax: null,
      stockMin: null,
      stockMax: null,
      stockStatus: [],
      pageNumber: 1,
      pageSize: 10,
      sortField: 'productID',
      sortOrder: 1
    },
    
    // Data
    products: [],
    totalCount: 0,
    loading: false,
    saving: false,
    
    // Dialogs
    showGalleryDialog: false,
    showFormDialog: false,
    isEditMode: false,
    
    // Gallery
    galleryImages: [],
    galleryTitle: '',
    
    // Form
    productForm: {
      productID: 0,
      sku: '',
      name: '',
      category: '',
      price: 0,
      quantityInStock: 0,
      description: ''
    },
    skuError: false,
    imageFiles: [],
    
    // Options
    viewOptions: [
      { label: 'Table View', value: 'table', icon: 'pi pi-table' },
      { label: 'Inline Edit', value: 'inline', icon: 'pi pi-pencil', disabled: true },
      { label: 'Tree View', value: 'tree', icon: 'pi pi-sitemap', disabled: true }
    ],
    
    categoryOptions: [
      'Electronics', 'Electronics2', 'Furniture', 'Lighting', 'Accessories'
    ],
    
    stockStatusOptions: [
      { label: 'In Stock', value: 'in-stock' },
      { label: 'Low Stock', value: 'low-stock' },
      { label: 'Out of Stock', value: 'out-of-stock' }
    ],
    
    pageSizeOptions: [5, 10, 25, 50]
  }
},
  mounted() {
    this.loadSavedFilters()
    this.search()
  },
  methods: {
    // View Management
    onViewChange(newView) {
      console.log('Switching to view:', newView)
      // Future: Handle view switching logic
      if (newView === 'inline') {
        alert('Coming Soon: Inline editing view will be available in the next version.')
      } else if (newView === 'tree') {
        alert('Coming Soon: Tree view will be available in the next version.')
      }
    },
    
    // Filter Management
    clearFilters() {
      this.filters = {
        name: '',
        category: '',
        sku: '',
        priceMin: null,
        priceMax: null,
        stockMin: null,
        stockMax: null,
        stockStatus: [],
        pageNumber: 1,
        pageSize: this.filters.pageSize,
        sortField: 'productID',
        sortOrder: 1
      }
      this.search()
    },
    
    saveFilters() {
      // Save current filters to localStorage for future sessions
      const filterState = {
        name: this.filters.name,
        category: this.filters.category,
        sku: this.filters.sku,
        priceMin: this.filters.priceMin,
        priceMax: this.filters.priceMax,
        stockMin: this.filters.stockMin,
        stockMax: this.filters.stockMax,
        stockStatus: this.filters.stockStatus,
        showAdvancedFilters: this.showAdvancedFilters
      }
      localStorage.setItem('productFilters', JSON.stringify(filterState))
      
      alert('Filter preferences have been saved.')
    },
    
    loadSavedFilters() {
      const savedFilters = localStorage.getItem('productFilters')
      if (savedFilters) {
        const filterState = JSON.parse(savedFilters)
        Object.assign(this.filters, filterState)
        this.showAdvancedFilters = filterState.showAdvancedFilters || false
      }
    },
    
    // Utility Methods
    getStockSeverity(stock) {
      if (stock === 0) return 'danger'
      if (stock <= 10) return 'warning'
      if (stock <= 50) return 'info'
      return 'success'
    },
    
    getStockStatus(stock) {
      if (stock === 0) return 'Out of Stock'
      if (stock <= 10) return 'Low Stock'
      return 'In Stock'
    },
    
    exportData() {
      // Future: Implement data export functionality
      alert('Data export functionality will be implemented soon.')
    },
    
    viewProduct(product) {
      // Open a read-only view of the product
      this.productForm = { ...product }
      this.isEditMode = true
      this.showFormDialog = true
    },
async checkDuplicateSKU(sku, currentId) {
  try {
    const res = await axios.get('https://localhost:7040/api/products/check-sku', {
      params: {
        sku,
        reserve: false,
        reservedBy: 'Dzmitry'
      }
    })

    return res.data.isTaken && res.data.productID !== currentId
  } catch (err) {
    console.warn('SKU check failed:', err)
    return false
  }
},
async generateSKU() {
  let sku
  let attempts = 0

  do {
    attempts++
    const suffix = Math.floor(1000 + Math.random() * 9000)
    const datePart = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    sku = `SKU-${datePart}-${suffix}`

    const res = await axios.get('https://localhost:7040/api/products/check-sku', {
      params: {
        sku,
        reserve: true,
        reservedBy: 'Dzmitry'
      }
    })

    if (!res.data.isTaken && res.data.reserved) {
      return sku
    }

  } while (attempts < 5)

  throw new Error('Could not generate unique SKU')
},
    editProduct(product) {
    this.productForm = { ...product }
    this.isEditMode = true
    this.showFormDialog = true
      this.skuError = false;
  },
async addProduct() {
  const res = await axios.get('https://localhost:7040/api/products/reserve-sku', {
    params: { reservedBy: 'Dzmitry' }
  })

  this.productForm = {
    productID: 0,
    sku: res.data.sku,
    name: '',
    category: '',
    price: 0,
    quantityInStock: 0,
    description: '',
    saleStartDate: null
  }

  this.skuError = false;
  this.isEditMode = false
  this.showFormDialog = true
},
async saveProduct() {
  try {
    this.saving = true
    this.skuError = false

    const sku = this.productForm.sku?.trim()

    if (sku) {
      const isDuplicate = await this.checkDuplicateSKU(sku, this.productForm.productID)
      if (isDuplicate) {
        this.skuError = true
        return
      }
    }

    // Collect images in Base64ImageDto[]
    let imageDtos = []

    if (!this.isEditMode && this.imageFiles?.length > 0) {
      const fileToBase64 = file =>
        new Promise(resolve => {
          const reader = new FileReader()
          reader.onload = () => resolve(reader.result)
          reader.readAsDataURL(file)
        })

      imageDtos = await Promise.all(
        this.imageFiles.map(async (file) => {
          const base64 = await fileToBase64(file)
          return {
            fileName: file.name,
            contentType: file.type,
            base64
          }
        })
      )
    }

    // Prepare payload
    const payload = {
      ...this.productForm,
      sku: sku || null,
      operationType: this.isEditMode ? 'Update' : 'Insert',
      images: imageDtos
    }

    // Send request
    await axios.post('https://localhost:7040/api/products/merge', payload)

    this.showFormDialog = false
    this.search()

    alert(`Product ${this.isEditMode ? 'updated' : 'created'} successfully.`)
  } catch (error) {
    console.error('Error saving product:', error)
    alert('Failed to save product. Please try again.')
  } finally {
    this.saving = false
  }
},

  confirmDelete(product) {
    if (confirm(`Are you sure you want to delete "${product.name}"? This action cannot be undone.`)) {
      axios
        .delete(`https://localhost:7040/api/products/${product.productID}`)
        .then(() => {
          this.search()
          alert(`Product "${product.name}" has been deleted.`)
        })
        .catch(error => {
          console.error('Delete error:', error)
          alert('Failed to delete product. Please try again.')
        })
    }
  },
    openGallery(product) {     
      this.galleryImages = product.images
      this.galleryTitle = product.name
      this.showGalleryDialog = true
    },
    search() {
      const payload = {
        ...this.filters,
        name: this.filters.name?.trim() || null,
        category: this.filters.category?.trim() || null,
        sku: this.filters.sku?.trim() || null,
        priceMin: this.filters.priceMin,
        priceMax: this.filters.priceMax,
        stockMin: this.filters.stockMin,
        stockMax: this.filters.stockMax,
        stockStatus: this.filters.stockStatus?.length > 0 ? this.filters.stockStatus : null
      }

      this.loading = true
      axios
        .post('https://localhost:7040/api/products/search', payload)
        .then(res => {
          this.products = res.data.items
          this.totalCount = res.data.totalCount
        })
        .catch(error => {
          console.error('Search error:', error)
          alert('Failed to load products. Please try again.')
        })
        .finally(() => (this.loading = false))
    },
    onPage(event) {
      this.filters.pageNumber = (event.page ?? 0) + 1
      this.search()
    },
    onSort(event) {
      this.filters.sortField = event.sortField
      this.filters.sortOrder = event.sortOrder
      this.search()
    }
  }
}

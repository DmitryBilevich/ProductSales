import axios from 'axios'
import { API_ENDPOINTS } from '../../config/api'
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
import Tree from 'primevue/tree'
import Calendar from 'primevue/calendar'
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
    Tree,
    Calendar,
    ProductImageManager,
    ImageUploader
  },
data() {
  return {
    // View Management
    currentView: 'table',
    selectedView: 'table',
    showAdvancedFilters: false,
    editingProducts: {},
    hasUnsavedChanges: false,
    bulkSaving: false,
    exportLoading: false,
    
    // Tree View
    treeNodes: [],
    expandedKeys: {},
    selectedKeys: {},
    selectedCategory: null,
    selectedProduct: null,
    categoryProducts: [],
    treeForm: {
      productID: 0,
      sku: '',
      name: '',
      category: '',
      price: 0,
      quantityInStock: 0,
      description: '',
      images: [],
      saleStartDate: null
    },
    
    // Quick search input
    quickSearchText: '',
    
    // Filters
    filters: {
      name: '',
      categories: [],
      sku: '',
      priceMin: null,
      priceMax: null,
      stockMin: null,
      stockMax: null,
      stockStatus: [],
      saleStartDateMin: null,
      saleStartDateMax: null,
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
      description: '',
      saleStartDate: null
    },
    skuError: false,
    imageFiles: [],
    
    // Import functionality
    importSessionId: null,
    importData: {
      items: [],
      totalCount: 0,
      summary: {
        totalRows: 0,
        newProducts: 0,
        updatedProducts: 0,
        errorRows: 0,
        lastModified: null
      },
      pageNumber: 1,
      pageSize: 10,
      sortField: 'RowNumber',
      sortOrder: 1,
      loading: false,
      processing: false
    },
    uploadProgress: {
      uploading: false,
      message: ''
    },
    editingImportItems: {},
    
    // Options
    viewOptions: [
      { label: 'Table View', value: 'table', icon: 'pi pi-table' },
      { label: 'Inline Edit', value: 'inline', icon: 'pi pi-pencil' },
      { label: 'Tree View', value: 'tree', icon: 'pi pi-sitemap' },
      { label: 'Load from File', value: 'import', icon: 'pi pi-upload' }
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
    onViewChange(event) {
      const newView = event.value
      console.log('Switching to view:', newView)
      
      // Tree view is now implemented
      
      // Check for unsaved changes before switching
      if (this.hasUnsavedChanges && newView !== this.currentView) {
        if (!confirm('You have unsaved changes. Are you sure you want to switch views? All changes will be lost.')) {
          // Reset the selected view to current view
          this.selectedView = this.currentView
          return
        }
        // Clear unsaved changes
        this.editingProducts = {}
        this.hasUnsavedChanges = false
      }
      
      // Update both current and selected view
      this.currentView = newView
      this.selectedView = newView
      
      if (newView === 'inline') {
        // Initialize inline editing mode
        this.initializeInlineEditing()
      } else if (newView === 'tree') {
        // Initialize tree view mode
        this.initializeTreeView()
      } else if (newView === 'import') {
        // Initialize import view mode
        this.initializeImportView()
      }
    },
    
    initializeInlineEditing() {
      // Clear any existing editing state
      this.editingProducts = {}
      this.hasUnsavedChanges = false
    },
    
    onQuickSearchInput() {
      // Quick search should not populate individual filter fields
      // The search() method will handle quickSearchText separately via nameOrSku parameter
    },
    
    onCategoryChange() {
      this.search()
    },
    
    // Filter Management
    clearFilters() {
      this.quickSearchText = ''
      this.filters = {
        name: '',
        categories: [],
        sku: '',
        priceMin: null,
        priceMax: null,
        stockMin: null,
        stockMax: null,
        stockStatus: [],
        saleStartDateMin: null,
        saleStartDateMax: null,
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
        quickSearchText: this.quickSearchText,
        name: this.filters.name,
        categories: this.filters.categories,
        sku: this.filters.sku,
        priceMin: this.filters.priceMin,
        priceMax: this.filters.priceMax,
        stockMin: this.filters.stockMin,
        stockMax: this.filters.stockMax,
        stockStatus: this.filters.stockStatus,
        saleStartDateMin: this.filters.saleStartDateMin,
        saleStartDateMax: this.filters.saleStartDateMax,
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
    
    formatDate(dateValue) {
      if (!dateValue) return 'Not set'
      const date = new Date(dateValue)
      // Use local date parts to avoid timezone issues
      return date.toLocaleDateString('en-US')
    },
    
    // Convert date to ISO string date part only (YYYY-MM-DD) to avoid timezone issues
    formatDateForServer(date) {
      if (!date) return null
      const d = new Date(date)
      // Get local date parts and format as YYYY-MM-DD
      const year = d.getFullYear()
      const month = String(d.getMonth() + 1).padStart(2, '0')
      const day = String(d.getDate()).padStart(2, '0')
      return `${year}-${month}-${day}`
    },
    
    // Parse date from server (expects YYYY-MM-DD or full datetime) and create local date
    parseDateFromServer(dateString) {
      if (!dateString) return null
      // Extract just the date part (YYYY-MM-DD) from datetime string
      const datePart = dateString.split('T')[0]
      const [year, month, day] = datePart.split('-').map(Number)
      // Create date in local timezone
      return new Date(year, month - 1, day)
    },
    
    
    viewProduct(product) {
      // Open a read-only view of the product
      this.productForm = { ...product }
      this.isEditMode = true
      this.showFormDialog = true
    },
    
    // Inline Editing Methods
    startInlineEdit(product) {
      // Create a copy of the product for editing with explicit values
      this.editingProducts[product.productID] = {
        productID: product.productID,
        sku: product.sku || '',
        name: product.name || '',
        category: product.category || '',
        price: Number(product.price) || 0,
        quantityInStock: Number(product.quantityInStock) || 0,
        description: product.description || '',
        images: product.images ? [...product.images] : [],
        saleStartDate: product.saleStartDate ? this.parseDateFromServer(product.saleStartDate) : null,
        isEditing: true,
        saving: false,
        originalValues: { 
          ...product, 
          images: product.images ? [...product.images] : [] 
        }
      }
      
      this.hasUnsavedChanges = true
    },
    
    cancelInlineEdit(productId) {
      if (this.editingProducts[productId]) {
        delete this.editingProducts[productId]
        this.checkUnsavedChanges()
      }
    },
    
    updateInlineField(productId, field, value) {
      if (this.editingProducts[productId]) {
        this.editingProducts[productId][field] = value
        this.hasUnsavedChanges = true
      }
    },
    
    checkUnsavedChanges() {
      this.hasUnsavedChanges = Object.keys(this.editingProducts).length > 0
    },
    
    getEditingProduct(product) {
      return this.editingProducts[product.productID] || product
    },
    
    isProductEditing(productId) {
      return this.editingProducts[productId]?.isEditing || false
    },
    
    getRowClass(data) {
      return {
        'editing-row': this.currentView === 'inline' && this.isProductEditing(data.productID)
      }
    },
    
    async saveAllChanges() {
      if (!this.hasUnsavedChanges) {
        alert('No changes to save.')
        return
      }
      
      try {
        this.bulkSaving = true
        const changedProducts = Object.values(this.editingProducts)
        
        if (changedProducts.length === 0) {
          alert('No changes to save.')
          return
        }
        
        // Save each changed product
        const savePromises = changedProducts.map(async product => {
          // Prepare images for save
          const imageDtos = []
          
          if (product.images && product.images.length > 0) {
            for (const image of product.images) {
              if (image.isNew) {
                // New image - send as base64
                imageDtos.push({
                  fileName: image.fileName,
                  contentType: image.contentType,
                  base64: image.imageUrl,
                  order: image.order
                })
              } else {
                // Existing image - just update order if needed
                imageDtos.push({
                  imageID: image.imageID,
                  order: image.order
                })
              }
            }
          }
          
          const payload = {
            productID: product.productID,
            sku: product.sku,
            name: product.name,
            category: product.category,
            price: Number(product.price) || 0,
            quantityInStock: Number(product.quantityInStock) || 0,
            description: product.description || '',
            saleStartDate: this.formatDateForServer(product.saleStartDate),
            operationType: 'Update',
            images: imageDtos
          }
          
          return axios.post(API_ENDPOINTS.PRODUCTS_MERGE, payload)
        })
        
        await Promise.all(savePromises)
        
        // Clear editing state
        this.editingProducts = {}
        this.hasUnsavedChanges = false
        
        // Refresh the table
        this.search()
        
        alert(`Successfully saved ${changedProducts.length} product(s).`)
        
      } catch (error) {
        console.error('Error saving changes:', error)
        alert('Failed to save some changes. Please check the console and try again.')
      } finally {
        this.bulkSaving = false
      }
    },
    
    async saveIndividualProduct(productId) {
      const product = this.editingProducts[productId]
      if (!product) {
        alert('No changes to save for this product.')
        return
      }
      
      try {
        // Set individual saving state
        product.saving = true
        
        // Prepare images for save
        const imageDtos = []
        
        if (product.images && product.images.length > 0) {
          for (const image of product.images) {
            if (image.isNew) {
              // New image - send as base64
              imageDtos.push({
                fileName: image.fileName,
                contentType: image.contentType,
                base64: image.imageUrl,
                order: image.order
              })
            } else {
              // Existing image - just update order if needed
              imageDtos.push({
                imageID: image.imageID,
                order: image.order
              })
            }
          }
        }
        
        const payload = {
          productID: product.productID,
          sku: product.sku,
          name: product.name,
          category: product.category,
          price: Number(product.price) || 0,
          quantityInStock: Number(product.quantityInStock) || 0,
          description: product.description || '',
          saleStartDate: this.formatDateForServer(product.saleStartDate),
          operationType: 'Update',
          images: imageDtos
        }
        
        await axios.post(API_ENDPOINTS.PRODUCTS_MERGE, payload)
        
        // Remove from editing state after successful save
        delete this.editingProducts[productId]
        this.checkUnsavedChanges()
        
        // Refresh the table to show updated data
        this.search()
        
        alert('Product saved successfully.')
        
      } catch (error) {
        console.error('Error saving product:', error)
        alert('Failed to save product. Please try again.')
      } finally {
        // Clear saving state
        if (this.editingProducts[productId]) {
          product.saving = false
        }
      }
    },
    
    discardAllChanges() {
      if (!this.hasUnsavedChanges) {
        return
      }
      
      if (confirm('Are you sure you want to discard all unsaved changes?')) {
        this.editingProducts = {}
        this.hasUnsavedChanges = false
        alert('All changes have been discarded.')
      }
    },
    
    // Image Management Methods
    moveImageUp(productId, index) {
      if (this.editingProducts[productId] && index > 0) {
        const images = [...this.editingProducts[productId].images]
        // Swap with previous image
        const temp = images[index]
        images[index] = images[index - 1]
        images[index - 1] = temp
        
        // Update order property
        images[index].order = index
        images[index - 1].order = index - 1
        
        this.editingProducts[productId].images = images
        this.hasUnsavedChanges = true
      }
    },
    
    moveImageDown(productId, index) {
      if (this.editingProducts[productId]) {
        const images = [...this.editingProducts[productId].images]
        if (index < images.length - 1) {
          // Swap with next image
          const temp = images[index]
          images[index] = images[index + 1]
          images[index + 1] = temp
          
          // Update order property
          images[index].order = index
          images[index + 1].order = index + 1
          
          this.editingProducts[productId].images = images
          this.hasUnsavedChanges = true
        }
      }
    },
    
    removeImage(productId, index) {
      if (this.editingProducts[productId]) {
        if (confirm('Are you sure you want to remove this image?')) {
          const images = [...this.editingProducts[productId].images]
          images.splice(index, 1)
          
          // Reorder remaining images
          images.forEach((image, idx) => {
            image.order = idx
          })
          
          this.editingProducts[productId].images = images
          this.hasUnsavedChanges = true
        }
      }
    },
    
    async addImageToProduct(productId, event) {
      const file = event.target.files[0]
      if (!file) return
      
      if (!this.editingProducts[productId]) return
      
      try {
        // Convert file to base64
        const base64 = await this.fileToBase64(file)
        const images = [...this.editingProducts[productId].images]
        
        // Create new image object
        const newImage = {
          imageID: `temp-${Date.now()}`, // Temporary ID
          imageUrl: base64,
          order: images.length,
          fileName: file.name,
          contentType: file.type,
          isNew: true // Flag to indicate this is a new image
        }
        
        images.push(newImage)
        this.editingProducts[productId].images = images
        this.hasUnsavedChanges = true
        
        // Clear the file input
        event.target.value = ''
        
      } catch (error) {
        console.error('Error adding image:', error)
        alert('Failed to add image. Please try again.')
      }
    },
    
    openFileDialog(productId) {
      const fileInput = document.getElementById(`fileInput-${productId}`)
      if (fileInput) {
        fileInput.click()
      }
    },
    
    fileToBase64(file) {
      return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload = () => resolve(reader.result)
        reader.onerror = error => reject(error)
        reader.readAsDataURL(file)
      })
    },
async checkDuplicateSKU(sku, currentId) {
  try {
    const res = await axios.get(API_ENDPOINTS.PRODUCTS_CHECK_SKU, {
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

    const res = await axios.get(API_ENDPOINTS.PRODUCTS_CHECK_SKU, {
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
  const res = await axios.get(API_ENDPOINTS.PRODUCTS_RESERVE_SKU, {
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
      saleStartDate: this.formatDateForServer(this.productForm.saleStartDate),
      operationType: this.isEditMode ? 'Update' : 'Insert',
      images: imageDtos
    }

    // Send request
    await axios.post(API_ENDPOINTS.PRODUCTS_MERGE, payload)

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
        .delete(`${API_ENDPOINTS.PRODUCTS}/${product.productID}`)
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
      // Calculate smart stock ranges
      const stockRanges = this.calculateStockRanges()
      
      const payload = {
        // Search fields (separate for advanced filters, unified for simple search)
        name: this.filters.name?.trim() || null,
        sku: this.filters.sku?.trim() || null,
        nameOrSku: this.quickSearchText?.trim() || null,
        categories: this.filters.categories?.length > 0 ? this.filters.categories : null,
        priceMin: this.filters.priceMin,
        priceMax: this.filters.priceMax,
        saleStartDateMin: this.formatDateForServer(this.filters.saleStartDateMin),
        saleStartDateMax: this.formatDateForServer(this.filters.saleStartDateMax),
        stockRanges: stockRanges.length > 0 ? stockRanges : null,
        // Pagination and sorting
        pageNumber: this.filters.pageNumber,
        pageSize: this.filters.pageSize,
        sortField: this.filters.sortField,
        sortOrder: this.filters.sortOrder
      }
      
      // Debug logging (can be removed in production)
      // console.log('=== SEARCH PAYLOAD ===')
      // console.log('stockRanges being sent:', payload.stockRanges)
      // console.log('=== END PAYLOAD ===')

      this.loading = true
      axios
        .post(API_ENDPOINTS.PRODUCTS_SEARCH, payload)
        .then(res => {
          this.products = res.data.items
          this.totalCount = res.data.totalCount
          
          // Rebuild tree if in tree view
          if (this.currentView === 'tree') {
            this.buildCategoryTree()
          }
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
    },
    
    // Tree View Methods
    initializeTreeView() {
      this.selectedCategory = null
      this.selectedProduct = null
      this.categoryProducts = []
      this.selectedKeys = {}
      this.buildCategoryTree()
    },
    
    buildCategoryTree() {
      // Group products by category
      const categoryGroups = {}
      
      this.products.forEach(product => {
        const category = product.category || 'Uncategorized'
        if (!categoryGroups[category]) {
          categoryGroups[category] = []
        }
        categoryGroups[category].push(product)
      })
      
      // Build tree nodes
      this.treeNodes = []
      
      // Add "All Products" root node
      this.treeNodes.push({
        key: 'all',
        label: 'All Products',
        icon: 'pi pi-box',
        count: this.products.length,
        children: []
      })
      
      // Add category nodes with product children
      Object.keys(categoryGroups).sort().forEach(category => {
        const categoryProducts = categoryGroups[category]
        const categoryNode = {
          key: `category-${category}`,
          label: category,
          icon: 'pi pi-folder',
          count: categoryProducts.length,
          data: { type: 'category', category: category },
          children: categoryProducts.map(product => ({
            key: `product-${product.productID}`,
            label: product.name,
            icon: 'pi pi-box',
            data: { type: 'product', product: product }
          }))
        }
        
        this.treeNodes.push(categoryNode)
      })
      
      // Auto-expand categories with few items
      this.expandedKeys = {}
      this.treeNodes.forEach(node => {
        if (node.children && node.children.length <= 5) {
          this.expandedKeys[node.key] = true
        }
      })
    },
    
    onNodeSelect(node) {
      if (node.data?.type === 'category') {
        this.selectedCategory = node.data.category
        this.selectedProduct = null
        this.loadCategoryProducts(node.data.category)
      } else if (node.data?.type === 'product') {
        this.selectedProduct = node.data.product
        this.selectedCategory = null
        this.categoryProducts = []
        this.loadProductForEditing(node.data.product)
      } else if (node.key === 'all') {
        this.selectedCategory = 'All'
        this.selectedProduct = null
        this.categoryProducts = [...this.products]
      }
    },
    
    onNodeUnselect() {
      this.selectedCategory = null
      this.selectedProduct = null
      this.categoryProducts = []
    },
    
    onNodeExpand(node) {
      this.expandedKeys[node.key] = true
    },
    
    onNodeCollapse(node) {
      delete this.expandedKeys[node.key]
    },
    
    expandAll() {
      this.expandedKeys = {}
      this.treeNodes.forEach(node => {
        this.expandedKeys[node.key] = true
      })
    },
    
    collapseAll() {
      this.expandedKeys = {}
    },
    
    loadCategoryProducts(category) {
      if (category === 'All') {
        this.categoryProducts = [...this.products]
      } else {
        this.categoryProducts = this.products.filter(p => p.category === category)
      }
    },
    
    selectProduct(product) {
      this.selectedProduct = product
      this.selectedCategory = null
      this.categoryProducts = []
      this.loadProductForEditing(product)
      
      // Update tree selection
      this.selectedKeys = {}
      this.selectedKeys[`product-${product.productID}`] = true
    },
    
    loadProductForEditing(product) {
      this.treeForm = {
        productID: product.productID,
        sku: product.sku || '',
        name: product.name || '',
        category: product.category || '',
        price: Number(product.price) || 0,
        quantityInStock: Number(product.quantityInStock) || 0,
        description: product.description || '',
        images: product.images ? [...product.images] : [],
        saleStartDate: product.saleStartDate ? this.parseDateFromServer(product.saleStartDate) : null
      }
    },
    
    async saveTreeProduct() {
      if (!this.selectedProduct) return
      
      try {
        this.saving = true
        
        // Prepare images for save
        const imageDtos = []
        
        if (this.treeForm.images && this.treeForm.images.length > 0) {
          for (const image of this.treeForm.images) {
            if (image.isNew) {
              // New image - send as base64
              imageDtos.push({
                fileName: image.fileName,
                contentType: image.contentType,
                base64: image.imageUrl,
                order: image.order
              })
            } else {
              // Existing image - just update order if needed
              imageDtos.push({
                imageID: image.imageID,
                order: image.order
              })
            }
          }
        }
        
        const payload = {
          productID: this.treeForm.productID,
          sku: this.treeForm.sku,
          name: this.treeForm.name,
          category: this.treeForm.category,
          price: Number(this.treeForm.price) || 0,
          quantityInStock: Number(this.treeForm.quantityInStock) || 0,
          description: this.treeForm.description || '',
          saleStartDate: this.formatDateForServer(this.treeForm.saleStartDate),
          operationType: this.selectedProduct.isNew ? 'Insert' : 'Update',
          images: imageDtos
        }
        
        await axios.post(API_ENDPOINTS.PRODUCTS_MERGE, payload)
        
        // Refresh data
        this.search()
        this.initializeTreeView() // Refresh tree structure
        
        const message = this.selectedProduct.isNew ? 'Product added successfully!' : 'Product saved successfully.'
        alert(message)
        
        // Clear selection after adding new product
        if (this.selectedProduct.isNew) {
          this.selectedProduct = null
        }
        
      } catch (error) {
        console.error('Error saving product:', error)
        alert('Failed to save product. Please try again.')
      } finally {
        this.saving = false
      }
    },
    
    cancelTreeEdit() {
      // Clear the product selection and go back to empty state
      this.selectedProduct = null
      this.selectedCategory = null
      this.categoryProducts = []
      this.selectedKeys = {}
      this.treeForm = {
        productID: 0,
        sku: '',
        name: '',
        category: '',
        price: 0,
        quantityInStock: 0,
        description: '',
        images: [],
        saleStartDate: null
      }
    },
    
    async addProductToCategory() {
      try {
        // Reserve a new SKU for the product
        const res = await axios.get(API_ENDPOINTS.PRODUCTS_RESERVE_SKU, {
          params: { reservedBy: 'User' }
        })

        // Set up tree form for new product
        this.treeForm = {
          productID: 0,
          sku: res.data.sku,
          name: '',
          category: this.selectedCategory && this.selectedCategory !== 'All' ? this.selectedCategory : '',
          price: 0,
          quantityInStock: 0,
          description: '',
          images: [],
          saleStartDate: null
        }
        
        // Create a dummy selectedProduct to show the form
        this.selectedProduct = { 
          productID: 0, 
          name: 'New Product',
          isNew: true 
        }
        
      } catch (error) {
        console.error('Error reserving SKU:', error)
        alert('Failed to create new product. Please try again.')
      }
    },
    
    // Tree Image Management
    moveTreeImageUp(index) {
      if (index > 0) {
        const images = [...this.treeForm.images]
        const temp = images[index]
        images[index] = images[index - 1]
        images[index - 1] = temp
        
        // Update order property
        images[index].order = index
        images[index - 1].order = index - 1
        
        this.treeForm.images = images
      }
    },
    
    moveTreeImageDown(index) {
      if (index < this.treeForm.images.length - 1) {
        const images = [...this.treeForm.images]
        const temp = images[index]
        images[index] = images[index + 1]
        images[index + 1] = temp
        
        // Update order property
        images[index].order = index
        images[index + 1].order = index + 1
        
        this.treeForm.images = images
      }
    },
    
    removeTreeImage(index) {
      if (confirm('Are you sure you want to remove this image?')) {
        const images = [...this.treeForm.images]
        images.splice(index, 1)
        
        // Reorder remaining images
        images.forEach((image, idx) => {
          image.order = idx
        })
        
        this.treeForm.images = images
      }
    },
    
    async addTreeImage(event) {
      const file = event.target.files[0]
      if (!file) return
      
      try {
        // Convert file to base64
        const base64 = await this.fileToBase64(file)
        const images = [...this.treeForm.images]
        
        // Create new image object
        const newImage = {
          imageID: `temp-${Date.now()}`,
          imageUrl: base64,
          order: images.length,
          fileName: file.name,
          contentType: file.type,
          isNew: true
        }
        
        images.push(newImage)
        this.treeForm.images = images
        
        // Clear the file input
        event.target.value = ''
        
      } catch (error) {
        console.error('Error adding image:', error)
        alert('Failed to add image. Please try again.')
      }
    },
    
    openTreeFileDialog() {
      const fileInput = document.getElementById('treeFileInput')
      if (fileInput) {
        fileInput.click()
      }
    },
    
    calculateStockRanges() {
      const stockRanges = []
      
      // Define status ranges
      const statusRanges = {
        'out-of-stock': { min: 0, max: 0 },
        'low-stock': { min: 1, max: 10 },
        'in-stock': { min: 11, max: null } // Open-ended: >= 11
      }
      
      // Debug logging (can be removed in production)
      // console.log('=== STOCK RANGE CALCULATION ===')
      // console.log('stockStatus:', this.filters.stockStatus)
      // console.log('stockMin:', this.filters.stockMin)
      // console.log('stockMax:', this.filters.stockMax)
      
      // Get stock status ranges
      const statusSelectedRanges = []
      if (this.filters.stockStatus?.length > 0) {
        this.filters.stockStatus.forEach(status => {
          const range = statusRanges[status]
          if (range) {
            statusSelectedRanges.push(range)
          }
        })
      }
      
      // Get manual stock range
      const manualRange = (this.filters.stockMin !== null || this.filters.stockMax !== null) ? {
        min: this.filters.stockMin || 0,
        max: this.filters.stockMax || null // Open-ended if no max specified
      } : null
      
      // Case 1: Only status filters
      if (statusSelectedRanges.length > 0 && !manualRange) {
        statusSelectedRanges.forEach(range => {
          stockRanges.push({ minStock: range.min, maxStock: range.max })
        })
      }
      // Case 2: Only manual range
      else if (!statusSelectedRanges.length && manualRange) {
        stockRanges.push({ minStock: manualRange.min, maxStock: manualRange.max })
      }
      // Case 3: Both status and manual range - find intersections
      else if (statusSelectedRanges.length > 0 && manualRange) {
        statusSelectedRanges.forEach(statusRange => {
          // Calculate intersection of manual range with each status range
          const intersectionMin = Math.max(manualRange.min, statusRange.min)
          
          // Handle null max values (open-ended ranges)
          let intersectionMax
          if (manualRange.max === null && statusRange.max === null) {
            intersectionMax = null // Both open-ended
          } else if (manualRange.max === null) {
            intersectionMax = statusRange.max // Manual is open-ended, use status max
          } else if (statusRange.max === null) {
            intersectionMax = manualRange.max // Status is open-ended, use manual max
          } else {
            intersectionMax = Math.min(manualRange.max, statusRange.max) // Both have max
          }
          
          // Only add if there's a valid intersection
          // For bounded ranges: min must be <= max
          // For open-ended ranges: always valid if intersectionMax is null
          const isValidIntersection = intersectionMax === null || intersectionMin <= intersectionMax
          
          // Debug logging for intersection calculation (can be removed in production)
          // console.log(`Intersection: Status(${statusRange.min}-${statusRange.max}) + Manual(${manualRange.min}-${manualRange.max}) = (${intersectionMin}-${intersectionMax}) Valid: ${isValidIntersection}`)
          
          if (isValidIntersection) {
            stockRanges.push({ minStock: intersectionMin, maxStock: intersectionMax })
          }
        })
      }
      
      // Special case: If we have stock filters active but no valid ranges, 
      // we need to send an impossible range to match nothing
      if (statusSelectedRanges.length > 0 && manualRange && stockRanges.length === 0) {
        stockRanges.push({ minStock: -1, maxStock: -1 }) // Impossible range - no stock can be negative
      }
      
      return stockRanges
    },
    
    // === IMPORT METHODS ===
    
    initializeImportView() {
      // Use fixed session ID for single user
      if (!this.importSessionId) {
        this.importSessionId = '11111111-1111-1111-1111-111111111111'
      }
      
      // Check if we have existing import data
      this.loadImportData()
    },
    
    async handleFileUpload(event) {
      const file = event.target.files[0]
      if (!file) return
      
      // Validate file type
      const allowedTypes = ['.xlsx', '.xls', '.csv']
      const fileExtension = '.' + file.name.split('.').pop().toLowerCase()
      
      if (!allowedTypes.includes(fileExtension)) {
        alert('Please select an Excel (.xlsx, .xls) or CSV file')
        return
      }
      
      // Validate file size (5MB)
      if (file.size > 5 * 1024 * 1024) {
        alert('File size must be less than 5MB')
        return
      }
      
      this.uploadProgress.uploading = true
      this.uploadProgress.message = 'Uploading and processing file...'
      
      try {
        const formData = new FormData()
        formData.append('file', file)
        
        const response = await axios.post(API_ENDPOINTS.PRODUCTS_UPLOAD, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        })
        
        if (response.data.success) {
          // Set a fixed session ID since we only have one user
          this.importSessionId = '11111111-1111-1111-1111-111111111111'
          this.uploadProgress.message = response.data.message
          await this.loadImportData()
          alert(`File uploaded successfully! ${response.data.summary?.totalRows || 0} rows processed.`)
        } else {
          alert('Error: ' + response.data.message)
        }
      } catch (error) {
        console.error('Upload error:', error)
        alert('Failed to upload file: ' + (error.response?.data?.message || error.message))
      } finally {
        this.uploadProgress.uploading = false
        this.uploadProgress.message = ''
        // Clear file input
        event.target.value = ''
      }
    },
    
    generateSessionId() {
      // Temporary method - the backend should return the session ID
      return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0
        const v = c == 'x' ? r : (r & 0x3 | 0x8)
        return v.toString(16)
      })
    },
    
    async loadImportData() {
      if (!this.importSessionId) return
      
      this.importData.loading = true
      
      try {
        const response = await axios.get(
          `${API_ENDPOINTS.PRODUCTS_IMPORT_STAGING}/${this.importSessionId}`,
          {
            params: {
              pageNumber: this.importData.pageNumber,
              pageSize: this.importData.pageSize,
              sortField: this.importData.sortField,
              sortOrder: this.importData.sortOrder
            }
          }
        )
        
        this.importData.items = response.data.items || []
        this.importData.totalCount = response.data.totalCount || 0
        this.importData.summary = response.data.summary || {}
        
      } catch (error) {
        console.error('Error loading import data:', error)
        if (error.response?.status === 404) {
          // No import data found, reset session
          this.importSessionId = null
          this.importData.items = []
          this.importData.totalCount = 0
        } else {
          alert('Failed to load import data: ' + (error.response?.data?.message || error.message))
        }
      } finally {
        this.importData.loading = false
      }
    },
    
    onImportPage(event) {
      this.importData.pageNumber = (event.page ?? 0) + 1
      this.loadImportData()
    },
    
    onImportSort(event) {
      this.importData.sortField = event.sortField
      this.importData.sortOrder = event.sortOrder
      this.loadImportData()
    },
    
    startImportEdit(item) {
      const stagingId = item.stagingId || item.stagingID  // Handle both naming conventions
      this.editingImportItems[stagingId] = {
        stagingId: stagingId,
        sku: item.sku || '',
        name: item.name || '',
        category: item.category || '',
        price: Number(item.price) || 0,
        quantityInStock: Number(item.quantityInStock) || 0,
        description: item.description || '',
        saleStartDate: item.saleStartDate ? this.parseDateFromServer(item.saleStartDate) : null,
        isEditing: true,
        saving: false,
        originalValues: { ...item }
      }
    },
    
    cancelImportEdit(stagingId) {
      delete this.editingImportItems[stagingId]
    },
    
    getImportEditingItem(item) {
      const stagingId = item.stagingId || item.stagingID
      return this.editingImportItems[stagingId] || item
    },
    
    isImportItemEditing(stagingId) {
      return this.editingImportItems[stagingId]?.isEditing || false
    },
    
    async saveImportItem(stagingId) {
      const item = this.editingImportItems[stagingId]
      if (!item) return
      
      item.saving = true
      
      try {
        const payload = {
          stagingID: item.stagingId,
          sku: item.sku,
          name: item.name,
          category: item.category,
          price: item.price,
          quantityInStock: item.quantityInStock,
          description: item.description,
          saleStartDate: this.formatDateForServer(item.saleStartDate)
        }
        
        const response = await axios.put(API_ENDPOINTS.PRODUCTS_UPDATE_STAGING, payload)
        
        if (response.data.success) {
          delete this.editingImportItems[stagingId]
          await this.loadImportData()
          alert('Item updated successfully')
        } else {
          alert('Error: ' + response.data.message)
        }
      } catch (error) {
        console.error('Error saving import item:', error)
        alert('Failed to save item: ' + (error.response?.data?.message || error.message))
      } finally {
        item.saving = false
      }
    },
    
    async deleteImportItem(stagingId) {
      if (!confirm('Are you sure you want to delete this import item?')) {
        return
      }
      
      try {
        const response = await axios.delete(`${API_ENDPOINTS.PRODUCTS_DELETE_STAGING}/${stagingId}`)
        
        if (response.data.success) {
          await this.loadImportData()
          alert('Item deleted successfully')
        } else {
          alert('Error: ' + response.data.message)
        }
      } catch (error) {
        console.error('Error deleting import item:', error)
        alert('Failed to delete item: ' + (error.response?.data?.message || error.message))
      }
    },
    
    async processImport() {
      if (!this.importSessionId) return
      
      if (!confirm(`Are you sure you want to process ${this.importData.summary.totalRows} products? This action cannot be undone.`)) {
        return
      }
      
      if (this.importData.summary.errorRows > 0) {
        alert(`Cannot process import: ${this.importData.summary.errorRows} rows have validation errors. Please fix them first.`)
        return
      }
      
      this.importData.processing = true
      
      try {
        const response = await axios.post(`${API_ENDPOINTS.PRODUCTS_PROCESS_IMPORT}/${this.importSessionId}`)
        
        if (response.data.success) {
          alert(`Import completed successfully! ${response.data.processedCount} products processed.`)
          this.importData.items = []
          this.importData.totalCount = 0
          this.importData.summary = {
            totalRows: 0,
            newProducts: 0,
            updatedProducts: 0,
            errorRows: 0,
            lastModified: null
          }
          // Refresh main product list
          this.search()
        } else {
          alert('Error processing import: ' + response.data.message)
        }
      } catch (error) {
        console.error('Error processing import:', error)
        alert('Failed to process import: ' + (error.response?.data?.message || error.message))
      } finally {
        this.importData.processing = false
      }
    },
    
    async clearImport() {
      if (!this.importSessionId) return
      
      if (!confirm('Are you sure you want to clear all import data? This action cannot be undone.')) {
        return
      }
      
      try {
        const response = await axios.delete(`${API_ENDPOINTS.PRODUCTS_CLEAR_IMPORT}/${this.importSessionId}`)
        
        if (response.data.success) {
          alert('Import data cleared successfully')
          this.importData.items = []
          this.importData.totalCount = 0
          this.importData.summary = {
            totalRows: 0,
            newProducts: 0,
            updatedProducts: 0,
            errorRows: 0,
            lastModified: null
          }
        } else {
          alert('Error clearing import: ' + response.data.message)
        }
      } catch (error) {
        console.error('Error clearing import:', error)
        alert('Failed to clear import: ' + (error.response?.data?.message || error.message))
      }
    },
    
    downloadTemplate() {
      window.open(API_ENDPOINTS.PRODUCTS_DOWNLOAD_TEMPLATE, '_blank')
    },
    
    // === EXPORT METHODS ===
    
    async exportProducts() {
      this.exportLoading = true
      
      try {
        const response = await axios.post(API_ENDPOINTS.PRODUCTS_EXPORT_TABLE, {
          ...this.filters,
          exportType: 'current'
        }, {
          responseType: 'blob'
        })
        
        this.downloadFile(response.data, `products_export_${new Date().toISOString().split('T')[0]}.xlsx`)
        
      } catch (error) {
        console.error('Export error:', error)
        alert('Failed to export products: ' + (error.response?.data?.message || error.message))
      } finally {
        this.exportLoading = false
      }
    },
    
    async exportTreeProducts() {
      this.exportLoading = true
      
      try {
        const payload = {
          exportType: 'tree',
          selectedCategory: this.selectedCategory
        }
        
        const response = await axios.post(API_ENDPOINTS.PRODUCTS_EXPORT_TREE, payload, {
          responseType: 'blob'
        })
        
        const filename = this.selectedCategory 
          ? `products_${this.selectedCategory}_${new Date().toISOString().split('T')[0]}.xlsx`
          : `all_products_${new Date().toISOString().split('T')[0]}.xlsx`
          
        this.downloadFile(response.data, filename)
        
      } catch (error) {
        console.error('Export error:', error)
        alert('Failed to export products: ' + (error.response?.data?.message || error.message))
      } finally {
        this.exportLoading = false
      }
    },
    
    async exportImportData() {
      this.exportLoading = true
      
      try {
        const response = await axios.post(API_ENDPOINTS.PRODUCTS_EXPORT_IMPORT, {
          importSessionId: this.importSessionId
        }, {
          responseType: 'blob'
        })
        
        this.downloadFile(response.data, `import_data_${new Date().toISOString().split('T')[0]}.xlsx`)
        
      } catch (error) {
        console.error('Export error:', error)
        alert('Failed to export import data: ' + (error.response?.data?.message || error.message))
      } finally {
        this.exportLoading = false
      }
    },
    
    downloadFile(blob, filename) {
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    },
    
    getImportOperationSeverity(operationType) {
      return operationType === 'Insert' ? 'success' : 'info'
    },
    
    getImportRowClass(data) {
      const classes = ['import-row']
      
      if (data.validationErrors) {
        classes.push('error-row')
      }
      
      if (this.isImportItemEditing(data.stagingId)) {
        classes.push('editing-row')
      }
      
      return classes.join(' ')
    }
  }
}

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
import Tree from 'primevue/tree'
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
      images: []
    },
    
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
      { label: 'Inline Edit', value: 'inline', icon: 'pi pi-pencil' },
      { label: 'Tree View', value: 'tree', icon: 'pi pi-sitemap' }
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
      }
    },
    
    initializeInlineEditing() {
      // Clear any existing editing state
      this.editingProducts = {}
      this.hasUnsavedChanges = false
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
            operationType: 'Update',
            images: imageDtos
          }
          
          return axios.post('https://localhost:7040/api/products/merge', payload)
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
          operationType: 'Update',
          images: imageDtos
        }
        
        await axios.post('https://localhost:7040/api/products/merge', payload)
        
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
        images: product.images ? [...product.images] : []
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
          operationType: 'Update',
          images: imageDtos
        }
        
        await axios.post('https://localhost:7040/api/products/merge', payload)
        
        // Refresh data
        this.search()
        
        alert('Product saved successfully.')
        
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
        images: []
      }
    },
    
    addProductToCategory() {
      // Use the existing addProduct functionality but pre-fill category
      this.addProduct()
      if (this.selectedCategory && this.selectedCategory !== 'All') {
        this.productForm.category = this.selectedCategory
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
    }
  }
}

import axios from 'axios'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Galleria from 'primevue/galleria'
import Dialog from 'primevue/dialog'
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
    Button,
    Galleria,
    Dialog,
    TabView,
    TabPanel,
    ProductImageManager,
    ImageUploader
  },
data() {
  return {
    filters: {
      name: '',
      category: '',
      pageNumber: 1,
      pageSize: 10,
      sortField: 'productID',
      sortOrder: 1
    },
    showGalleryDialog: false,
    galleryImages: [],
    galleryTitle: '',
    products: [],
    totalCount: 0,
    loading: false,
        showFormDialog: false,
    isEditMode: false,
    productForm: {
      productID: 0,
      name: '',
      category: '',
      price: 0,
      quantityInStock: 0,
      description: ''
    },
    skuError: false,
    imageFiles: []
  }
},
  mounted() {
    this.search()
  },
  methods: {
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
  this.skuError = false;

  const sku = this.productForm.sku?.trim();

  if (sku) {
    const isDuplicate = await this.checkDuplicateSKU(sku, this.productForm.productID);
    if (isDuplicate) {
      this.skuError = true;
      return;
    }
  }

  // 🔽 Сбор изображений в Base64ImageDto[]
  let imageDtos = [];

  if (!this.isEditMode && this.imageFiles?.length > 0) {
    const fileToBase64 = file =>
      new Promise(resolve => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.readAsDataURL(file);
      });

    imageDtos = await Promise.all(
      this.imageFiles.map(async (file) => {
        const base64 = await fileToBase64(file);
        return {
          fileName: file.name,
          contentType: file.type,
          base64
        };
      })
    );
  }

  // 🔽 Подготовка объекта к отправке
  const payload = {
    ...this.productForm,
    sku: sku || null,
    operationType: this.isEditMode ? 'Update' : 'Insert',
    images: imageDtos
  };

  // 🔽 Отправка
  await axios.post('https://localhost:7040/api/products/merge', payload);

  this.showFormDialog = false;
  this.search();
},

  confirmDelete(product) {
    if (confirm(`Delete ${product.name}?`)) {
      axios
        .delete(`https://localhost:7040/api/products/${product.productID}`)
        .then(() => this.search())
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
        category: this.filters.category?.trim() || null
      }

      this.loading = true
      axios
        .post('https://localhost:7040/api/products/search', payload)
        .then(res => {
          this.products = res.data.items
          this.totalCount = res.data.totalCount
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

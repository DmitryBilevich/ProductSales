import axios from 'axios'

export default {
  name: 'OrderList',
  data() {
    return {
      filters: {
        fromDate: '',
        toDate: '',
        customerName: '',
        productIDs: []
      },
      products: [],
      orders: []
    }
  },
  mounted() {
    axios.get('https://localhost:7040/api/products')
      .then(res => this.products = res.data)

    this.search()
  },
  methods: {
    search() {
      const payload = {
        ...this.filters,
        fromDate: this.filters.fromDate || null,
        toDate: this.filters.toDate || null,
        customerName: this.filters.customerName?.trim() || null,
        productIDs: this.filters.productIDs
      }

      axios.post('https://localhost:7040/api/orders/search', payload)
        .then(res => this.orders = res.data)
    },
    formatDate(_, __, cellValue) {
      return new Date(cellValue).toLocaleDateString()
    }
  }
}

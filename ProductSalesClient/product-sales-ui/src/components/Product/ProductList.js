import axios from 'axios'

export default {
  name: 'ProductGrid',
  data() {
    return {
      filters: {
        name: '',
        category: '',
        pageNumber: 1,
        pageSize: 10
      },
      products: [],
      totalCount: 0,
      loading: false
    }
  },
  mounted() {
    this.search()
  },
  methods: {
    search() {

  const payload = {
    ...this.filters,
    name: this.filters.name?.trim() || null,
    category: this.filters.category?.trim() || null
  }


      this.loading = true
      axios.post('https://localhost:7040/api/products/search', payload)
        .then(res => {
          this.products = res.data.items
          this.totalCount = res.data.totalCount
        })
        .catch(err => {
          console.error(err)
          this.products = []
        })
        .finally(() => {
          this.loading = false
        })
    },
    changePage(page) {
      this.filters.pageNumber = page
      this.search()
    }
  }
}

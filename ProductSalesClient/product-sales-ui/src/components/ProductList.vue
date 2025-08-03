<template>
  <div>
    <h2>Products</h2>
    <table border="1" cellpadding="8">
      <thead>
        <tr>
          <th>Name</th>
          <th>Category</th>
          <th>Price</th>
          <th>In Stock</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="product in products" :key="product.productID">
          <td>{{ product.name }}</td>
          <td>{{ product.category }}</td>
          <td>{{ product.price }}</td>
          <td>{{ product.quantityInStock }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'ProductList',
  data() {
    return {
      products: []
    }
  },
  mounted() {
    axios.get('https://localhost:7040/api/products')
      .then(response => {
        this.products = response.data
      })
      .catch(error => {
        console.error('API error:', error)
      })
  }
}
</script>

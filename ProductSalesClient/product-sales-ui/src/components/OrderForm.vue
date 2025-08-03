<template>
  <div>
    <h2>Create Order</h2>

    <form @submit.prevent="submitOrder">
      <div>
        <label>Customer Name:</label>
        <input v-model="order.customerName" required />
      </div>
      <div>
        <label>Phone:</label>
        <input v-model="order.customerPhone" />
      </div>
      <div>
        <label>Email:</label>
        <input v-model="order.customerEmail" />
      </div>

      <h3>Order Items</h3>
      <div v-for="(item, index) in order.items" :key="index">
        <select v-model="item.productID">
          <option disabled value="">Select product</option>
          <option v-for="product in products" :key="product.productID" :value="product.productID">
            {{ product.name }} ({{ product.price }}$)
          </option>
        </select>
        <input type="number" min="1" v-model.number="item.quantity" placeholder="Qty" />
        <button @click.prevent="removeItem(index)">🗑</button>
      </div>

      <button @click.prevent="addItem">+ Add Product</button>
      <br /><br />
      <button type="submit">Submit Order</button>
    </form>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'OrderForm',
  data() {
    return {
      products: [],
      order: {
        customerName: '',
        customerPhone: '',
        customerEmail: '',
        items: []
      }
    }
  },
  mounted() {
    axios.get('https://localhost:7040/api/products')
      .then(res => {
        this.products = res.data
      })
      .catch(err => console.error(err))
  },
  methods: {
    addItem() {
      this.order.items.push({ productID: '', quantity: 1 })
    },
    removeItem(index) {
      this.order.items.splice(index, 1)
    },
    submitOrder() {
      // Преобразуем строковые числа в int
      const payload = {
        ...this.order,
        items: this.order.items.map(item => ({
          productID: parseInt(item.productID),
          quantity: parseInt(item.quantity)
        }))
      }

      axios.post('https://localhost:7040/api/orders', payload)
        .then(() => {
          alert('Order placed!')
          this.resetForm()
        })
        .catch(err => {
          console.error(err)
          alert('Failed to submit order')
        })
    },
    resetForm() {
      this.order = {
        customerName: '',
        customerPhone: '',
        customerEmail: '',
        items: []
      }
    }
  }
}
</script>

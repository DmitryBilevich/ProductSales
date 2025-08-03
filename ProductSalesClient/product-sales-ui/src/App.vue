<template>
  <div id="app">
    <h1>Product Sales UI</h1>
    <nav>
      <router-link to="/products">Products</router-link> |
      <router-link to="/orders/create">New Order</router-link> |
      <router-link to="/orders">Orders</router-link>
    </nav>
    <hr />
    <router-view />
  </div>
</template>

<script>
export default { name: 'App' }
</script>

<style scoped>
nav a {
  margin-right: 1rem;
  text-decoration: none;
}
nav a.router-link-exact-active {
  font-weight: bold;
  color: darkgreen;
}
</style>

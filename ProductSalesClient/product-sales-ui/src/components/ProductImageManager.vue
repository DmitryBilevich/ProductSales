<template>
  <div v-if="productId">
    <h4 class="p-mb-2">Images</h4>

    <!-- Превью миниатюр -->
    <div class="image-manager">
      <div
        class="image-thumb"
        v-for="img in images"
        :key="img.imageID"
      >
        <img :src="img.imageUrl" alt="product image" />
        <Button
          icon="pi pi-trash"
          class="p-button-sm p-button-danger p-button-rounded"
          @click="deleteImage(img.imageID)"
        />
      </div>
    </div>

    <!-- Компонент для загрузки с crop -->
    <div class="p-mt-3">
      <ImageCropUpload :productId="productId" @uploaded="loadImages" />
    </div>
  </div>
</template>

<script>
import axios from 'axios'
import Button from 'primevue/button'
import ImageCropUpload from './ImageCropUpload.vue'

export default {
  name: 'ProductImageManager',
  components: {
    Button,
    ImageCropUpload
  },
  props: {
    productId: {
      type: Number,
      required: true
    }
  },
  data() {
    return {
      images: []
    }
  },
  mounted() {
    this.loadImages()
  },
  methods: {
    loadImages() {
      axios
        .get(`https://localhost:7040/api/products/${this.productId}/images`)
        .then(res => {
          this.images = res.data
        })
    },
    deleteImage(imageId) {
      if (!confirm('Delete this image?')) return

      axios
        .delete(`https://localhost:7040/api/products/images/${imageId}`)
        .then(() => this.loadImages())
    }
  }
}
</script>

<style scoped>
.image-manager {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.image-thumb {
  position: relative;
  width: 100px;
  height: 70px;
  border: 1px solid #ccc;
  border-radius: 4px;
  overflow: hidden;
}

.image-thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.image-thumb .p-button {
  position: absolute;
  top: 0;
  right: 0;
  z-index: 1;
}
</style>

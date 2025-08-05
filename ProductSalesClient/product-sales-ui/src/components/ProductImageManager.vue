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
        <img :src="getImageSrc(img)" alt="product image" />
        <Button
          icon="pi pi-trash"
          class="p-button-sm p-button-danger p-button-rounded"
          @click="deleteImage(img.imageID)"
        />
      </div>
    </div>

    <!-- Компонент для загрузки с crop -->
    <div class="p-mt-3">
      <ImageCropUpload :productId="productId" @image-added="handleImageAdded" />
    </div>
  </div>
</template>

<script>
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
    },
    images: {
      type: Array,
      default: () => []
    }
  },
  methods: {
    deleteImage(imageId) {
      if (!confirm('Delete this image?')) return
      // Emit event to parent to handle image deletion
      this.$emit('image-deleted', imageId)
    },
    handleImageAdded(imageData) {
      // Emit event to parent to handle image addition
      this.$emit('image-added', imageData)
    },
    getImageSrc(img) {
      // Handle base64 data (new format)
      if (img.imageData) {
        const contentType = img.contentType || 'image/jpeg'
        return `data:${contentType};base64,${img.imageData}`
      }
      // Handle legacy imageUrl format (if any still exist)
      if (img.imageUrl) {
        return img.imageUrl
      }
      // Fallback placeholder
      return 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTAwIiBoZWlnaHQ9IjEwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZGRkIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzk5OSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPk5vIEltYWdlPC90ZXh0Pjwvc3ZnPg=='
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

<template>
  <div>
    <input type="file" @change="onFileChange" accept="image/*" />

    <div v-if="imageUrl" style="margin-top: 1rem;">
      <p style="margin-bottom: 0.5rem; color: #666; font-size: 0.9rem;">
        Adjust the crop area and click "Add to Product" to include this image:
      </p>
      <cropper
        :src="imageUrl"
        :stencil-props="{ aspectRatio: 4 / 3 }"
        ref="cropper"
        class="cropper"
      />
      <div style="margin-top: 1rem;">
        <Button label="Add to Product" icon="pi pi-plus" @click="addCroppedImage" class="p-button-success" />
        <Button label="Discard" icon="pi pi-times" class="p-button-secondary" @click="reset" />
      </div>
    </div>
  </div>
</template>

<script>
import { Cropper } from 'vue-advanced-cropper'
import 'vue-advanced-cropper/dist/style.css'
import Button from 'primevue/button'

export default {
  name: 'ImageCropUpload',
  components: {
    Cropper,
    Button
  },
  props: {
    productId: { type: Number, required: true }
  },
  data() {
    return {
      imageUrl: null,
      originalFile: null
    }
  },
  methods: {
    onFileChange(event) {
      const file = event.target.files[0]
      if (!file) return

      this.originalFile = file
      this.imageUrl = URL.createObjectURL(file)
    },
    async addCroppedImage() {
      const canvas = this.$refs.cropper.getResult().canvas
      if (!canvas) return alert('Crop area not selected.')

      // Convert canvas to base64 instead of uploading to server
      const base64Data = canvas.toDataURL('image/jpeg', 0.8) // 0.8 quality
      
      // Emit the image data to parent component
      this.$emit('image-added', {
        fileName: this.originalFile.name,
        base64Data: base64Data,
        contentType: 'image/jpeg'
      })

      // Show success feedback
      alert('Image added to product successfully!')
      
      this.reset()
    },
    reset() {
      this.imageUrl = null
      this.originalFile = null
    }
  }
}
</script>

<style scoped>
.cropper {
  width: 100%;
  max-width: 400px;
  height: auto;
  border: 1px solid #ccc;
  border-radius: 6px;
  margin-top: 1rem;
}
</style>

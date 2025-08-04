<template>
  <div>
    <input type="file" @change="onFileChange" accept="image/*" />

    <div v-if="imageUrl" style="margin-top: 1rem;">
      <cropper
        :src="imageUrl"
        :stencil-props="{ aspectRatio: 4 / 3 }"
        ref="cropper"
        class="cropper"
      />
      <div style="margin-top: 1rem;">
        <Button label="Save & Upload" @click="uploadCropped" />
        <Button label="Cancel" class="p-button-secondary" @click="reset" />
      </div>
    </div>
  </div>
</template>

<script>
import { Cropper } from 'vue-advanced-cropper'
import 'vue-advanced-cropper/dist/style.css'
import Button from 'primevue/button'
import axios from 'axios'

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
    async uploadCropped() {
      const canvas = this.$refs.cropper.getResult().canvas
      if (!canvas) return alert('Crop area not selected.')

      canvas.toBlob(async blob => {
        const formData = new FormData()
        formData.append('file', blob, this.originalFile.name)

        await axios.post(
          `https://localhost:7040/api/products/${this.productId}/images`,
          formData
        )

        this.reset()
        this.$emit('uploaded')
      }, 'image/jpeg')
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

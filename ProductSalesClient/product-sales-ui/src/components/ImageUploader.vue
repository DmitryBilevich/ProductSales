<template>
  <div class="image-uploader">
    <div class="image-list">
      <div
        class="image-preview"
        v-for="(file, index) in files"
        :key="index"
      >
        <img :src="file.preview" alt="Preview" />
        <Button
          icon="pi pi-trash"
          class="p-button-sm p-button-danger"
          @click="remove(index)"
        />
      </div>
    </div>

    <input
      type="file"
      accept="image/*"
      multiple
      @change="onSelect"
    />
  </div>
</template>

<script>
import Button from 'primevue/button'

export default {
  name: 'ImageUploader',
  components: { Button },
  props: {
    initialImages: {
      type: Array,
      default: () => []
    }
  },
  data() {
    return {
      files: [] // { file: File, preview: string }
    }
  },
  methods: {
    onSelect(event) {
      const selectedFiles = Array.from(event.target.files || [])
      const filePromises = selectedFiles.map(file => {
        return new Promise(resolve => {
          const reader = new FileReader()
          reader.onload = () => {
            resolve({ file, preview: reader.result })
          }
          reader.readAsDataURL(file)
        })
      })

      Promise.all(filePromises).then(previews => {
        this.files.push(...previews)
        this.emitUpdate()
      })
    },
    remove(index) {
      this.files.splice(index, 1)
      this.emitUpdate()
    },
    emitUpdate() {
      const rawFiles = this.files.map(f => f.file)
      this.$emit('update', rawFiles)
    },
    clear() {
      this.files = []
      this.emitUpdate()
    }
  }
}
</script>

<style scoped>
.image-uploader {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.image-list {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1rem;
}

.image-preview {
  position: relative;
  width: 100px;
  height: 70px;
  border: 1px solid #ccc;
  border-radius: 4px;
  overflow: hidden;
}

.image-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.image-preview .p-button {
  position: absolute;
  top: 0;
  right: 0;
  z-index: 2;
}
</style>

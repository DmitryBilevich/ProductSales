<template>
  <div v-if="images.length">
    <Galleria
      :value="images"
      :responsiveOptions="responsiveOptions"
      :numVisible="3"
      containerStyle="max-width: 600px"
      :showThumbnails="true"
      :showIndicators="images.length > 1"
      :circular="true"
      :autoPlay="false"
    >
      <template #item="slotProps">
        <img
          :src="slotProps.item.url"
          :alt="slotProps.item.alt"
          style="width: 100%; display: block"
        />
      </template>
      <template #thumbnail="slotProps">
        <img
          :src="slotProps.item.url"
          :alt="slotProps.item.alt"
          style="width: 60px"
        />
      </template>
    </Galleria>
  </div>
  <div v-else>
    <p>No images available for this product.</p>
  </div>
</template>

<script>
import Galleria from 'primevue/galleria'
import axios from 'axios'

export default {
  name: 'ProductImageGallery',
  components: {
    Galleria
  },
  props: {
    productId: {
      type: Number,
      required: true
    }
  },
  data() {
    return {
      images: [],
      responsiveOptions: [
        {
          breakpoint: '1024px',
          numVisible: 3
        },
        {
          breakpoint: '768px',
          numVisible: 2
        },
        {
          breakpoint: '560px',
          numVisible: 1
        }
      ]
    }
  },
  mounted() {
    axios
      .get(`https://localhost:7040/api/products/${this.productId}/images`)
      .then(res => {
        this.images = res.data.map(img => ({
          url: `/images/products/${img.fileName}`,
          alt: `Image ${img.imageID}`
        }))
      })
      .catch(err => console.error(err))
  }
}
</script>

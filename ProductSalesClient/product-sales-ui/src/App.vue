<template>
  <div id="app">
    <!-- Header with Brand and Navigation -->
    <div class="layout-topbar">
      <div class="layout-topbar-content">
        <div class="layout-topbar-logo">
          <i class="pi pi-shopping-cart" style="font-size: 2rem; color: var(--primary-color)"></i>
          <span class="layout-topbar-title">Gift Shop Manager</span>
        </div>
        
        <Menubar :model="menuItems" class="layout-topbar-menu">
          <template #start>
            <div class="flex align-items-center gap-2">
              <i class="pi pi-home"></i>
            </div>
          </template>
          <template #end>
            <div class="flex align-items-center gap-2">
              <Avatar 
                icon="pi pi-user" 
                class="mr-2" 
                style="background-color: var(--primary-color); color: var(--primary-color-text)"
              />
              <span class="font-medium">Admin</span>
            </div>
          </template>
        </Menubar>
      </div>
    </div>

    <!-- Breadcrumb -->
    <div class="layout-breadcrumb">
      <Breadcrumb :model="breadcrumbItems" class="mb-3">
        <template #item="{ item }">
          <router-link v-if="item.route" :to="item.route" class="text-decoration-none">
            <span :class="[item.icon, 'mr-2']"></span>
            <span class="text-color">{{ item.label }}</span>
          </router-link>
          <span v-else class="text-color-secondary">
            <span :class="[item.icon, 'mr-2']"></span>
            <span>{{ item.label }}</span>
          </span>
        </template>
      </Breadcrumb>
    </div>

    <!-- Main Content Area -->
    <div class="layout-main">
      <div class="layout-content">
        <router-view />
      </div>
    </div>

    <!-- Footer -->
    <div class="layout-footer">
      <div class="layout-footer-content">
        <span class="font-medium">Gift Shop Management System</span>
        <span class="text-color-secondary ml-auto">© 2025</span>
      </div>
    </div>
  </div>
</template>

<script>
import Menubar from 'primevue/menubar'
import Avatar from 'primevue/avatar'
import Breadcrumb from 'primevue/breadcrumb'

export default {
  name: 'App',
  components: {
    Menubar,
    Avatar,
    Breadcrumb
  },
  data() {
    return {
      menuItems: [
        {
          label: 'Products',
          icon: 'pi pi-fw pi-box',
          route: '/products'
        },
        {
          label: 'Orders',
          icon: 'pi pi-fw pi-shopping-bag',
          items: [
            {
              label: 'View Orders',
              icon: 'pi pi-fw pi-list',
              route: '/orders'
            },
            {
              label: 'Create Order',
              icon: 'pi pi-fw pi-plus',
              route: '/orders/create'
            }
          ]
        },
        {
          label: 'Reports',
          icon: 'pi pi-fw pi-chart-bar',
          items: [
            {
              label: 'Sales Report',
              icon: 'pi pi-fw pi-chart-line'
            },
            {
              label: 'Inventory Report',
              icon: 'pi pi-fw pi-database'
            }
          ]
        }
      ]
    }
  },
  computed: {
    breadcrumbItems() {
      const items = [{ icon: 'pi pi-home', route: '/' }]
      
      if (this.$route.path.includes('/products')) {
        items.push({ label: 'Products', icon: 'pi pi-box' })
      } else if (this.$route.path.includes('/orders')) {
        items.push({ label: 'Orders', icon: 'pi pi-shopping-bag' })
        if (this.$route.path.includes('/create')) {
          items.push({ label: 'Create Order', icon: 'pi pi-plus' })
        }
      }
      
      return items
    }
  }
}
</script>

<style>
/* Global Styles */
#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: var(--surface-ground);
}

/* Header Styles */
.layout-topbar {
  background: var(--surface-card);
  border-bottom: 1px solid var(--surface-border);
  padding: 0;
  position: sticky;
  top: 0;
  z-index: 1000;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.layout-topbar-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 2rem;
  min-height: 4rem;
}

.layout-topbar-logo {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.layout-topbar-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-color);
  margin: 0;
}

.layout-topbar-menu {
  border: none;
  background: transparent;
  padding: 0;
}

/* Breadcrumb Area */
.layout-breadcrumb {
  background: var(--surface-ground);
  padding: 1rem 2rem 0;
}

/* Main Content */
.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.layout-content {
  flex: 1;
  padding: 1.5rem 2rem;
  max-width: 100%;
}

/* Footer */
.layout-footer {
  background: var(--surface-card);
  border-top: 1px solid var(--surface-border);
  margin-top: auto;
}

.layout-footer-content {
  display: flex;
  align-items: center;
  padding: 1rem 2rem;
  color: var(--text-color-secondary);
  font-size: 0.875rem;
}

/* Responsive Design */
@media screen and (max-width: 768px) {
  .layout-topbar-content {
    padding: 0 1rem;
  }
  
  .layout-content {
    padding: 1rem;
  }
  
  .layout-breadcrumb {
    padding: 1rem 1rem 0;
  }
  
  .layout-topbar-title {
    font-size: 1.25rem;
  }
  
  .layout-footer-content {
    padding: 1rem;
  }
}

@media screen and (max-width: 480px) {
  .layout-topbar-logo span {
    display: none;
  }
  
  .layout-topbar-menu .p-menubar-start,
  .layout-topbar-menu .p-menubar-end span {
    display: none;
  }
}
</style>

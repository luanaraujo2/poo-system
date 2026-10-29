<template>
  <Toast position="top-center" />
  <!-- A chave faz a tela recarregar ao trocar entre "Nova tarefa" e "Editar tarefa", que usam o mesmo componente. -->
  <RouterView :key="$route.fullPath" />
</template>

<script setup>
import Toast from 'primevue/toast'
</script>

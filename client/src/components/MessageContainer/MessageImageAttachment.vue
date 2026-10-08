<script setup>
defineProps({
    imageUrl: {
        type: String,
        required: true
    },
    alt: {
        type: String,
        default: "Image Attachment"
    }
});

const emit = defineEmits(["open"]);
</script>

<template>
    <div class="mb-2">
        <img 
            :src="imageUrl" 
            :alt="alt" 
            class="img-fluid rounded border border-secondary border-opacity-25"
            style="max-height: 300px; object-fit: contain; width: 100%; cursor: zoom-in;"
            @click.stop="emit('open')"
            title="Click to expand"
        />
    </div>
</template>
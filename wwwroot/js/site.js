// Dropzone click to open file picker
document.addEventListener('DOMContentLoaded', function () {
    const dropzone = document.getElementById('dropzone');
    const fileInput = document.getElementById('pdfFile');
    if (dropzone && fileInput) {
        dropzone.addEventListener('click', () => fileInput.click());
    }
});

// HTML editor of the table of contents (TinyMCE, self-hosted under the GPL license).
// The allowed elements match the server-side sanitizer (HtmlTableOfContentsConverter),
// so what the user sees in the editor is what gets stored.
(function () {
    'use strict';

    tinymce.init({
        selector: 'textarea.toc-editor',
        license_key: 'gpl',
        base_url: '/lib/tinymce',
        suffix: '.min',
        promotion: false,
        branding: false,
        menubar: false,
        height: 420,
        plugins: 'lists advlist code',
        toolbar: 'undo redo | blocks | bold italic underline strikethrough | bullist numlist | outdent indent | removeformat | code',
        block_formats: 'Paragraph=p; Heading 1=h1; Heading 2=h2; Heading 3=h3; Heading 4=h4',
        valid_elements: 'h1,h2,h3,h4,h5,h6,p,ul,ol,li,strong/b,em/i,u,s,br,div,span',
        entity_encoding: 'raw',
        skin: window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oxide-dark' : 'oxide',
        content_css: window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'default'
    });
})();

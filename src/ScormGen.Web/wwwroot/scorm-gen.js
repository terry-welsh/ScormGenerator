window.scormGen = {
    triggerDownload: function (url) {
        const a = document.createElement('a');
        a.href = url;
        a.download = 'scorm_packages.zip';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    },
    downloadText: function (content, filename, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};

window.scormGen = {
    triggerDownload: function (url) {
        const a = document.createElement('a');
        a.href = url;
        a.download = 'scorm_packages.zip';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
};

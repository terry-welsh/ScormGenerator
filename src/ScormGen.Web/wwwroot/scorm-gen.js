window.scormGen = {
    submitAndDownload: async function (fileBytes, fileName) {
        const blob = new Blob([new Uint8Array(fileBytes)], { type: 'application/json' });
        const formData = new FormData();
        formData.append('course', blob, fileName);

        const response = await fetch('/generate', { method: 'POST', body: formData });
        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || `Server error: ${response.status}`);
        }

        const zipBlob = await response.blob();
        const url = URL.createObjectURL(zipBlob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'scorm_packages.zip';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};

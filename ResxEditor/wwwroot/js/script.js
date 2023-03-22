window.downloadFileFromArray = (fileName, arrayBuffer) => {
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

window.toggleModal = (modalName, mode) => {
    $(modalName).modal(mode);
}

window.openFilePicker = () => {
    document.getElementById('filePicker').click();
}

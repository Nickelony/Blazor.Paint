function getBoundingClientRect(element) {
	return element.getBoundingClientRect();
}

function getScrollWidth(element) {
	return element.scrollWidth;
}

function getScrollHeight(element) {
	return element.scrollHeight;
}

function getImageWidth(imageVar) {
	const image = window[imageVar];

	return image.decode().then(() => {
		return image.width;
	});
}

function getImageHeight(imageVar) {
	const image = window[imageVar];

	return image.decode().then(() => {
		return image.height;
	});
}

function triggerFileDownload(fileName, url) {
	const anchorElement = document.createElement('a');
	anchorElement.download = fileName ?? '';
	anchorElement.href = url;
	anchorElement.click();
	anchorElement.remove();
}

function downloadTextFile(fileName, text) {
	const anchorElement = document.createElement('a');
	anchorElement.download = fileName ?? '';
	anchorElement.href = "data:text/plain;charset=utf-8," + encodeURIComponent(text);
	anchorElement.click();
	anchorElement.remove();
}

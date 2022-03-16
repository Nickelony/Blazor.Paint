/* Classes */

class DotNetSerializableImageData {
	constructor(imageData) {
		this.data = new Uint8Array(imageData.data);
		this.width = imageData.width;
		this.height = imageData.height
		this.colorSpace = imageData.colorSpace;
	}
}

/* Properties */

function getFillStyle(id) { return window[id].fillStyle; }
function getGlobalAlpha(id) { return window[id].globalAlpha; }
function getGlobalCompositeOperation(id) { return window[id].globalCompositeOperation; }
function getLineCap(id) { return window[id].lineCap; }
function getLineDashOffset(id) { return window[id].lineDashOffset; }
function getLineJoin(id) { return window[id].lineJoin; }
function getLineWidth(id) { return window[id].lineWidth; }
function getStrokeStyle(id) { return window[id].strokeStyle; }

function setFillStyle(id, style) { window[id].fillStyle = style; }
function setGlobalAlpha(id, alpha) { window[id].globalAlpha = alpha; }
function setGlobalCompositeOperation(id, operation) { window[id].globalCompositeOperation = operation; }
function setLineCap(id, cap) { window[id].lineCap = cap; }
function setLineDashOffset(id, offset) { window[id].lineDashOffset = offset; }
function setLineJoin(id, join) { window[id].lineJoin = join; }
function setLineWidth(id, width) { window[id].lineWidth = width; }
function setStrokeStyle(id, style) { window[id].strokeStyle = style; }

/* Methods */

function doArc(id, x, y, radius, startAngle, endAngle, counterclockwise) { window[id].arc(x, y, radius, startAngle, endAngle, counterclockwise); }
function doBeginPath(id) { window[id].beginPath(); }
function doClearRect(id, x, y, width, height) { window[id].clearRect(x, y, width, height); }
function doClosePath(id) { window[id].closePath(); }
function doEllipse(id, x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise) { window[id].ellipse(x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise); }
function doFill(id) { window[id].fill(); }
function doFillRect(id, x, y, width, height) { window[id].fillRect(x, y, width, height); }
function doLineTo(id, x, y) { window[id].lineTo(x, y); }
function doMoveTo(id, x, y) { window[id].moveTo(x, y); }
function doRect(id, x, y, width, height) { window[id].rect(x, y, width, height); }
function doRestore(id) { window[id].restore(); }
function doRotate(id, angle) { window[id].rotate(angle); }
function doSave(id) { window[id].save(); }
function doStroke(id) { window[id].stroke(); }
function doTranslate(id, x, y) { window[id].translate(x, y); }

function getLineDash(id) { return window[id].getLineDash(); }
function setLineDash(id, segments) { window[id].setLineDash(segments); }

function getImageData(id, x, y, width, height) {
	return new DotNetSerializableImageData(window[id].getImageData(x, y, width, height));
}

function putImageData(id, data, imageWidth, imageHeight, colorSpace, x, y) {
	window[id].putImageData(new ImageData(new Uint8ClampedArray(data), imageWidth, imageHeight, { colorSpace: colorSpace }), x, y);
}

function doDrawImage(id, jsVarName, x, y) { window[id].drawImage(window[jsVarName], x, y) }
function doDrawImage2(id, jsVarName, x, y, width, height) { window[id].drawImage(window[jsVarName], x, y, width, height); }

function initContext2D(canvasId, givenContextId) {
	eval(`${givenContextId} = document.getElementById("${canvasId}").getContext("2d", { willReadFrequently: true })`);
}

function toDataURL(id) {
	return document.getElementById(id).toDataURL();
}

function toDataURL2(id, format) {
	return document.getElementById(id).toDataURL(format);
}

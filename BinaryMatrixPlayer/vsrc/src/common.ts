
function onDocLoad(fn: () => void, doc: Document = document) {
	if(doc.readyState == "loading")
		doc.addEventListener("DOMContentLoaded", fn, { once: true, passive: true })
	else
		fn();
}

function apply<T>(v: T, fn: (value: T) => void) : T {
	fn(v);
	return v;
}


export {
	onDocLoad,
	apply
}

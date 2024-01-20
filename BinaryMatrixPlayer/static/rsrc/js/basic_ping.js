


document.addEventListener("DOMContentLoaded", () => {

	const wsURL = new URL("/connect-socket", document.baseURI);
	wsURL.protocol = "ws://";

	const socket = new WebSocket(wsURL);

	const output = document.getElementById("output");
	const error = document.getElementById("error")

	socket.addEventListener("error", (e) => {
		error.appendChild(new Text("An error occured during communication.\n"));
	});

	socket.addEventListener("close", (e) => {
		error.appendChild(new Text("Close frame received with code:" + e.code + "\n"));
	});

	socket.addEventListener("message", (e) => {
		const span = document.createElement("span");
		span.appendChild(new Text(e.data));
		output.appendChild(span);
		output.appendChild(new Text("\n"));
	});

	socket.addEventListener("open", (e) => {
		error.appendChild(new Text("WebSocket opened."));
		socket.send("HELLO SERVER!!!1");
	})

	document.getElementById("ping-form").addEventListener("submit", (ev) => {
		ev.preventDefault();
		if(socket.readyState === WebSocket.OPEN) {
			const data = new FormData(ev.target);
			socket.send(data.get("data"));
		} else {
			error.appendChild(new Text("socket not connected"));
		}
	});

})

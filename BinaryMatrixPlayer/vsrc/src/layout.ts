/* import { createSignal, createEffect } from "solid-js"; */
import { apply, onDocLoad } from "./common";

type CardValue = "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "a" | "*" | "?" | ">" | "@";
type CardAxiom = "+" | "%" | "&" | "!" | "^" | "#";

function createCard() : HTMLElement;
function createCard(value : CardValue, axiom : CardAxiom) : HTMLElement;

function createCard(value? : CardValue, axiom? : CardAxiom) : HTMLElement {
	const el = document.createElement("div");
	el.classList.add("game-card");

	if(!value) {
		el.classList.add("--is-unknown");
		el.appendChild(new Text("X"));
	} else {
		el.classList.add("--axiom-" + axiom);
		el.classList.add("--value-" + value);
		el.appendChild(apply(document.createElement("span"), (valueEl) => {
			valueEl.classList.add("-value")
			valueEl.appendChild(new Text(value));
		}))
		el.appendChild(apply(document.createElement("span"), (valueEl) => {
			valueEl.classList.add("-axiom")
			valueEl.appendChild(new Text(axiom));
		}))
	}

	return el;
}

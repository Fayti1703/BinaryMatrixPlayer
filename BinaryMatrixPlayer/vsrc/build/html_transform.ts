import { Plugin } from "vite";


const BoardGenerator = {
	name: "binmat-board-generator",
	transformIndexHtml: {
		transform(html, context) {
			return html.replace(/(\t+)<!-- %GAME BOARD% -->/, (_, indent) =>  {
				let rowNames = [ "a", "d", "l", "x" ];
				let out = "";
				for(let laneNo = 0; laneNo < 6; laneNo++) {
					out += `${indent}<div class="game-lane">`;
					for(let rowNo = 0; rowNo < 4; rowNo++) {
						out += `\n${indent}\t<div class="game-cell" data-cell-name="${rowNames[rowNo]}${laneNo}"></div>`;
					}
					out += `\n${indent}</div>\n`;
				}

				out += `\
${indent}<div class="game-lane --is-pseudo">
${indent}   <div class="game-cell" data-cell-name="da"></div>
${indent}   <div class="game-cell" data-cell-name="xa"></div>
${indent}</div>`;

				return out;
			})
		}
	}
} satisfies Plugin;

export {
	BoardGenerator
}

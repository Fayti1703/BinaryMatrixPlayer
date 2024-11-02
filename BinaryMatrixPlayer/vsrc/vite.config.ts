import { defineConfig } from "vite";
import { resolve as pathResolve } from "path";
import { nodeResolve } from "@rollup/plugin-node-resolve"
import { BoardGenerator } from "./build/html_transform";

/* do not try to run a vite dev server from this, it will not work */
export default defineConfig({
	appType: "custom",
	build: {
		outDir: "../static",
		manifest: "vite.manifest.json",
		emptyOutDir: true,
		sourcemap: true,
		rollupOptions: {
			input: {
				index: pathResolve(__dirname, "index.html"),
				layout: pathResolve(__dirname, "layout.html")
			},
			plugins: [
				nodeResolve({
					extensions: [ ".ts" ]
				})
			]
		}
	},
	plugins: [
		BoardGenerator
	]
})

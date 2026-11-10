// PDF.js's viewer components find the library through globalThis.pdfjsLib, so it has to be set before they're loaded. This
// module is imported first for that reason
import * as pdfjsLib from "pdfjs-dist";

globalThis.pdfjsLib = pdfjsLib;

#!/usr/bin/env node
/**
 * Auditoria de layout mobile: abre as páginas do app num viewport de celular e
 * mede quem está mais largo que a tela.
 *
 * Ler o CSS não responde "por que a página tem barra horizontal" — o culpado é
 * sempre um elemento cujo min-content não cabe, e isso só o layout diz. Este
 * script pergunta ao navegador.
 *
 * Sem dependências: usa o Chrome instalado e o WebSocket nativo do Node 22.
 *
 *   node audit.mjs                       # 390x844, páginas padrão
 *   node audit.mjs --width 360 --page /search
 */

import { spawn } from "node:child_process";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const args = process.argv.slice(2);
const flag = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 ? args[i + 1] : fallback;
};

const WIDTH = Number(flag("width", 390));
const HEIGHT = Number(flag("height", 844));
const BASE = flag("base", "http://localhost:4200");
const PAGES = flag("page") ? [flag("page")] : ["/home", "/search", "/resultado"];
const CHROME = flag("chrome", "google-chrome");
// Screenshot só sai correto por aqui: `--window-size` sozinho esbarra na largura
// mínima de janela do Chrome (~500px) e mente sobre o layout do celular.
const SHOT_DIR = flag("shot", null);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// --- sobe um Chrome descartável com o protocolo de depuração aberto
const profile = mkdtempSync(join(tmpdir(), "mobile-audit-"));
const chrome = spawn(CHROME, [
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  "--no-sandbox",
  "--remote-debugging-port=0",
  `--user-data-dir=${profile}`,
  "about:blank",
], { stdio: ["ignore", "pipe", "pipe"] });

const wsUrl = await new Promise((resolve, reject) => {
  let buffer = "";
  const timer = setTimeout(() => reject(new Error("Chrome não abriu o debugger")), 20000);
  chrome.stderr.on("data", (chunk) => {
    buffer += chunk;
    const match = buffer.match(/ws:\/\/[^\s]+/);
    if (match) { clearTimeout(timer); resolve(match[0]); }
  });
});

const ws = new WebSocket(wsUrl);
await new Promise((resolve) => ws.addEventListener("open", resolve));

let nextId = 1;
const pending = new Map();
ws.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    const { resolve, reject } = pending.get(message.id);
    pending.delete(message.id);
    message.error ? reject(new Error(JSON.stringify(message.error))) : resolve(message.result);
  }
});

const send = (method, params = {}, sessionId) =>
  new Promise((resolve, reject) => {
    const id = nextId++;
    pending.set(id, { resolve, reject });
    ws.send(JSON.stringify({ id, method, params, sessionId }));
  });

// --- uma aba, emulando celular
const { targetId } = await send("Target.createTarget", { url: "about:blank" });
const { sessionId } = await send("Target.attachToTarget", { targetId, flatten: true });
await send("Page.enable", {}, sessionId);
await send("Runtime.enable", {}, sessionId);
await send("Emulation.setDeviceMetricsOverride", {
  width: WIDTH, height: HEIGHT, deviceScaleFactor: 2, mobile: true,
}, sessionId);

/** Roda no contexto da página: acha todo elemento que estoura a largura da tela. */
const PROBE = `(() => {
  const vw = document.documentElement.clientWidth;
  const label = (el) => {
    const id = el.id ? '#' + el.id : '';
    const cls = typeof el.className === 'string' && el.className
      ? '.' + el.className.trim().split(/\\s+/).slice(0, 3).join('.') : '';
    return el.tagName.toLowerCase() + id + cls;
  };
  const offenders = [];
  for (const el of document.querySelectorAll('*')) {
    const rect = el.getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) continue;
    const overflowsRight = Math.round(rect.right) > vw + 1;
    const tooWide = Math.round(rect.width) > vw + 1;
    if (!overflowsRight && !tooWide) continue;
    const style = getComputedStyle(el);
    offenders.push({
      el: label(el),
      w: Math.round(rect.width),
      left: Math.round(rect.left),
      right: Math.round(rect.right),
      scrollW: el.scrollWidth,
      // Um pai com overflow auto/hidden absorve o estouro: o culpado é quem NÃO tem.
      overflowX: style.overflowX,
      whiteSpace: style.whiteSpace,
      minWidth: style.minWidth,
      depth: (() => { let d = 0, p = el; while ((p = p.parentElement)) d++; return d; })(),
    });
  }
  // O elemento mais profundo que estoura é a causa; os pais só herdam.
  offenders.sort((a, b) => b.depth - a.depth || b.w - a.w);
  const touch = [...document.querySelectorAll('button, a, input, select, [role=button]')]
    .map((el) => ({ el: label(el), ...el.getBoundingClientRect().toJSON() }))
    .filter((r) => r.width > 0 && (r.height < 40 || r.width < 40))
    .map((r) => ({ el: r.el, w: Math.round(r.width), h: Math.round(r.height) }));
  return JSON.stringify({
    viewport: vw,
    documentScrollWidth: document.documentElement.scrollWidth,
    bodyScrollWidth: document.body.scrollWidth,
    offenders: offenders.slice(0, 14),
    smallTargets: touch.slice(0, 12),
  });
})()`;

for (const path of PAGES) {
  await send("Page.navigate", { url: BASE + path }, sessionId);
  await sleep(3500); // Angular renderiza e as chamadas de API assentam

  const { result } = await send("Runtime.evaluate", {
    expression: PROBE, returnByValue: true,
  }, sessionId);
  const data = JSON.parse(result.value);

  const overflow = data.documentScrollWidth - data.viewport;
  console.log("\n" + "=".repeat(78));
  console.log(`${path}   viewport ${data.viewport}px   scrollWidth ${data.documentScrollWidth}px` +
    (overflow > 1 ? `   ⚠ ESTOURA ${overflow}px` : "   ok"));
  console.log("=".repeat(78));

  if (data.offenders.length) {
    console.log("elemento".padEnd(42) + "larg".padStart(6) + "dir".padStart(6) +
      "  overflow-x  white-space");
    for (const o of data.offenders) {
      console.log(o.el.slice(0, 41).padEnd(42) + String(o.w).padStart(6) +
        String(o.right).padStart(6) + "  " + o.overflowX.padEnd(11) + o.whiteSpace);
    }
  } else {
    console.log("nenhum elemento mais largo que a tela");
  }

  if (SHOT_DIR) {
    const { data: png } = await send("Page.captureScreenshot",
      { format: "png", captureBeyondViewport: true }, sessionId);
    const file = `${SHOT_DIR}/${path.replace(/\//g, "_") || "root"}-${WIDTH}.png`;
    writeFileSync(file, Buffer.from(png, "base64"));
    console.log(`screenshot: ${file}`);
  }

  if (data.smallTargets.length) {
    console.log("\nalvos de toque abaixo de 40px (WCAG 2.5.8 pede 24, iOS recomenda 44):");
    for (const t of data.smallTargets) {
      console.log(`  ${t.el.slice(0, 46).padEnd(48)} ${t.w}x${t.h}`);
    }
  }
}

ws.close();
chrome.kill();

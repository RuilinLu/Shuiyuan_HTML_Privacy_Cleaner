import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const htmlPath = resolve(process.argv[2]);
const screenshotPath = process.argv[3] ? resolve(process.argv[3]) : "";
const profileDir = resolve(".edge-cdp-layout-check");
const port = 9333 + Math.floor(Math.random() * 300);
const fileUrl = "file:///" + htmlPath.replace(/\\/g, "/");
mkdirSync(profileDir, { recursive: true });

const child = spawn(edge, [
  "--headless",
  "--disable-gpu",
  "--no-first-run",
  `--remote-debugging-port=${port}`,
  `--user-data-dir=${profileDir}`,
  "--window-size=1365,1800",
  fileUrl,
], { stdio: "ignore" });

function delay(ms) {
  return new Promise((resolveDelay) => setTimeout(resolveDelay, ms));
}

async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`${response.status} ${response.statusText}`);
  return response.json();
}

async function waitForTarget() {
  for (let i = 0; i < 80; i++) {
    try {
      const targets = await fetchJson(`http://127.0.0.1:${port}/json`);
      const page = targets.find((target) => target.type === "page" && target.webSocketDebuggerUrl);
      if (page) return page;
    } catch {
      // Browser may still be starting.
    }
    await delay(100);
  }
  throw new Error("CDP target did not start");
}

async function connect(wsUrl) {
  const ws = new WebSocket(wsUrl);
  await new Promise((resolveOpen, rejectOpen) => {
    ws.addEventListener("open", resolveOpen, { once: true });
    ws.addEventListener("error", rejectOpen, { once: true });
  });

  let nextId = 1;
  const pending = new Map();
  ws.addEventListener("message", async (event) => {
    let raw = event.data;
    if (raw && typeof raw.text === "function") {
      raw = await raw.text();
    } else if (Buffer.isBuffer(raw)) {
      raw = raw.toString("utf8");
    } else if (raw instanceof ArrayBuffer) {
      raw = Buffer.from(raw).toString("utf8");
    }
    const message = JSON.parse(String(raw));
    if (message.id && pending.has(message.id)) {
      const { resolveMessage, rejectMessage } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) rejectMessage(new Error(JSON.stringify(message.error)));
      else resolveMessage(message.result);
    }
  });

  return {
    send(method, params = {}) {
      const id = nextId++;
      const response = new Promise((resolveMessage, rejectMessage) => {
        pending.set(id, { resolveMessage, rejectMessage });
      });
      ws.send(JSON.stringify({ id, method, params }));
      return response;
    },
    close() {
      ws.close();
    },
  };
}

try {
  const target = await waitForTarget();
  const cdp = await connect(target.webSocketDebuggerUrl);
  await cdp.send("Page.enable");
  await cdp.send("Runtime.enable");
  await delay(1200);
  await cdp.send("Runtime.evaluate", { expression: "window.scrollTo(0,0)", awaitPromise: false });
  await delay(500);

  const expression = `(() => {
    const rect = (el) => {
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { top: Math.round(r.top), left: Math.round(r.left), width: Math.round(r.width), height: Math.round(r.height) };
    };
    return {
      scrollY: Math.round(window.scrollY),
      title: rect(document.querySelector('#topic-title')),
      stream: rect(document.querySelector('.post-stream')),
      posts: Array.from(document.querySelectorAll('.topic-post')).slice(0, 10).map((post) => ({
        n: post.getAttribute('data-post-number'),
        post: rect(post),
        row: rect(post.querySelector('.post__row')),
        avatar: rect(post.querySelector('.topic-avatar')),
        body: rect(post.querySelector('.topic-body')),
        name: post.querySelector('.anonymous-names')?.textContent?.trim(),
        text: post.querySelector('.cooked')?.textContent?.trim()?.slice(0, 50),
      })),
    };
  })()`;
  const layout = await cdp.send("Runtime.evaluate", { expression, returnByValue: true });
  console.log(JSON.stringify(layout.result.value, null, 2));

  if (screenshotPath) {
    const image = await cdp.send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
    writeFileSync(screenshotPath, Buffer.from(image.data, "base64"));
  }
  cdp.close();
} finally {
  child.kill();
}

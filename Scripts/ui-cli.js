#!/usr/bin/env node

/**
 * Unity UI Quality of Life CLI
 * Automates spawning and building UI components into Unity from web code, JSON specifications, or images.
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const { parseWebCodeToSpec } = require('./spec-parser');

const DEFAULT_PORT = 8765;
const QUEUE_DIR = path.resolve(__dirname, '../Assets/_Project/Pipeline/Queue');
const FINISHED_DIR = path.resolve(__dirname, '../Assets/_Project/Pipeline/Finished');

async function httpRequest(options, postData = null) {
  return new Promise((resolve, reject) => {
    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => (data += chunk));
      res.on('end', () => {
        try {
          resolve({ status: res.statusCode, body: JSON.parse(data) });
        } catch {
          resolve({ status: res.statusCode, body: data });
        }
      });
    });

    req.on('error', (err) => reject(err));
    req.setTimeout(4000, () => {
      req.destroy();
      reject(new Error('Connection timed out.'));
    });

    if (postData) {
      req.write(typeof postData === 'string' ? postData : JSON.stringify(postData));
    }
    req.end();
  });
}

async function isServerRunning() {
  try {
    const res = await httpRequest({
      hostname: '127.0.0.1',
      port: DEFAULT_PORT,
      path: '/api/status',
      method: 'GET'
    });
    return res.status === 200;
  } catch {
    return false;
  }
}

async function sendBuildRequest(spec) {
  const serverUp = await isServerRunning();

  if (serverUp) {
    const res = await httpRequest(
      {
        hostname: '127.0.0.1',
        port: DEFAULT_PORT,
        path: '/api/build',
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      },
      spec
    );
    return res.body;
  } else {
    // Fallback: write to file queue
    console.log('[UI CLI] Unity HTTP bridge offline. Using file-queue fallback...');
    if (!fs.existsSync(QUEUE_DIR)) fs.mkdirSync(QUEUE_DIR, { recursive: true });
    if (!fs.existsSync(FINISHED_DIR)) fs.mkdirSync(FINISHED_DIR, { recursive: true });

    const timestamp = Date.now();
    const cleanName = (spec.name || 'Component').replace(/[^a-zA-Z0-9_-]/g, '_');
    const queueFile = path.join(QUEUE_DIR, `${timestamp}_${cleanName}.json`);
    const resultFile = path.join(FINISHED_DIR, `${timestamp}_${cleanName}_result.json`);

    fs.writeFileSync(queueFile, JSON.stringify(spec, null, 2), 'utf-8');
    console.log(`[UI CLI] Enqueued spec: ${queueFile}`);
    console.log('[UI CLI] Waiting for Unity Editor to process queue...');

    // Wait up to 3 seconds for result file unless --nowait specified
    if (spec._nowait) {
      return { success: true, message: `Spec written to queue: ${queueFile}` };
    }

    const start = Date.now();
    while (Date.now() - start < 3500) {
      if (fs.existsSync(resultFile)) {
        const raw = fs.readFileSync(resultFile, 'utf-8');
        try {
          return JSON.parse(raw);
        } catch {
          return { success: true, message: raw };
        }
      }
      await new Promise((r) => setTimeout(r, 350));
    }

    return {
      success: true,
      message: 'Spec written to queue. Unity Editor will process it upon next frame.'
    };
  }
}

async function handleBuild(args) {
  let spec = null;

  if (args.spec) {
    const specPath = path.resolve(process.cwd(), args.spec);
    if (!fs.existsSync(specPath)) {
      console.error(`[UI CLI Error] Spec file not found at: ${specPath}`);
      process.exit(1);
    }
    spec = JSON.parse(fs.readFileSync(specPath, 'utf-8'));
  } else if (args.code || args['code-file']) {
    let rawCode = args.code || '';
    const codeFilePath = args['code-file'] || (fs.existsSync(rawCode) ? rawCode : null);
    if (codeFilePath && fs.existsSync(codeFilePath)) {
      rawCode = fs.readFileSync(codeFilePath, 'utf-8');
    }
    const name = args.name || 'WebComponent';
    const category = args.category || 'Web';
    spec = parseWebCodeToSpec(rawCode, name, [category]);
  } else if (args.json) {
    spec = JSON.parse(args.json);
  } else {
    console.error('[UI CLI Error] Must specify --spec <file>, --code <html>, or --json <rawJson>');
    process.exit(1);
  }

  console.log(`[UI CLI] Building component: '${spec.name}'...`);
  const result = await sendBuildRequest(spec);
  console.log('[UI CLI] Result:', JSON.stringify(result, null, 2));
}

async function handleSpawn(args) {
  if (!args.name) {
    console.error('[UI CLI Error] Must specify --name <PresetName>');
    process.exit(1);
  }

  try {
    const res = await httpRequest(
      {
        hostname: '127.0.0.1',
        port: DEFAULT_PORT,
        path: '/api/spawn',
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      },
      args.name
    );
    console.log('[UI CLI] Spawn Result:', res.body);
  } catch (err) {
    console.error(`[UI CLI Error] Could not connect to Unity: ${err.message}`);
  }
}

async function handleList() {
  try {
    const res = await httpRequest({
      hostname: '127.0.0.1',
      port: DEFAULT_PORT,
      path: '/api/presets',
      method: 'GET'
    });
    console.log('[UI CLI] Presets in Unity Project:');
    console.log(JSON.stringify(res.body, null, 2));
  } catch (err) {
    console.error(`[UI CLI Error] Unity HTTP bridge offline: ${err.message}`);
  }
}

async function handleStatus() {
  const up = await isServerRunning();
  console.log(`[UI CLI] Unity Editor Bridge Status: ${up ? 'ONLINE (Port ' + DEFAULT_PORT + ')' : 'OFFLINE'}`);
}

function parseArgs(rawArgs) {
  const parsed = { command: rawArgs[0] };
  for (let i = 1; i < rawArgs.length; i++) {
    const arg = rawArgs[i];
    if (arg.startsWith('--')) {
      const key = arg.slice(2);
      const next = rawArgs[i + 1];
      if (next && !next.startsWith('--')) {
        parsed[key] = next;
        i++;
      } else {
        parsed[key] = true;
      }
    }
  }
  return parsed;
}

async function main() {
  const rawArgs = process.argv.slice(2);
  if (rawArgs.length === 0) {
    console.log(`
UI Quality of Life - Component Pipeline CLI

Usage:
  node Scripts/ui-cli.js build --spec <path/to/spec.json>
  node Scripts/ui-cli.js build --code "<button ...>Click</button>" --name "PrimaryBtn" --category "Buttons"
  node Scripts/ui-cli.js build --json '{"name":"Card", ...}'
  node Scripts/ui-cli.js spawn --name "PrimaryBtn"
  node Scripts/ui-cli.js list
  node Scripts/ui-cli.js status
`);
    return;
  }

  const args = parseArgs(rawArgs);

  switch (args.command) {
    case 'build':
      await handleBuild(args);
      break;
    case 'spawn':
      await handleSpawn(args);
      break;
    case 'list':
      await handleList();
      break;
    case 'status':
      await handleStatus();
      break;
    default:
      console.error(`Unknown command: ${args.command}`);
  }
}

main().catch((err) => {
  console.error('[UI CLI Fatal Error]:', err);
  process.exit(1);
});

#!/usr/bin/env node

/**
 * Unity UI Quality of Life CLI
 * Automates spawning and building UI components, color themes, and CSS animations
 * into Unity from web code, specifications, or design tokens.
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');
const { parseWebCodeToSpec } = require('./spec-parser');
const { parseCssThemeTokens, parseCssAnimation } = require('./styling-parser');

function loadProjectSettings() {
  const settingsFile = path.resolve(__dirname, '../ProjectSettings/UIQualityLifeSettings.asset');
  let port = 8765;
  let queueDir = path.resolve(__dirname, '../Assets/_Project/Pipeline/Queue');
  let finishedDir = path.resolve(__dirname, '../Assets/_Project/Pipeline/Finished');

  if (fs.existsSync(settingsFile)) {
    try {
      const content = fs.readFileSync(settingsFile, 'utf-8');
      const portMatch = content.match(/m_BridgePort:\s*(\d+)/);
      if (portMatch) port = parseInt(portMatch[1], 10);

      const queueMatch = content.match(/m_QueueFolder:\s*(.+)/);
      if (queueMatch && queueMatch[1].trim()) {
        queueDir = path.resolve(__dirname, '..', queueMatch[1].trim());
      }

      const finishedMatch = content.match(/m_FinishedFolder:\s*(.+)/);
      if (finishedMatch && finishedMatch[1].trim()) {
        finishedDir = path.resolve(__dirname, '..', finishedMatch[1].trim());
      }
    } catch {
      // Ignore and use defaults
    }
  }

  return { port, queueDir, finishedDir };
}

const projectSettings = loadProjectSettings();
const DEFAULT_PORT = projectSettings.port;
const QUEUE_DIR = projectSettings.queueDir;
const FINISHED_DIR = projectSettings.finishedDir;

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
    req.setTimeout(15000, () => {
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

function tryOfficialUnityCommand(csharpCode) {
  try {
    const raw = execFileSync('unity.exe', ['command', 'eval', csharpCode], {
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'ignore'],
      timeout: 15000
    });
    
    // Look for JSON result in stdout
    const match = raw.match(/"result":\s*("[^"]+"|\{[^}]+\}|true|false|[0-9]+)/);
    if (match) {
      let resVal = match[1];
      if (resVal.startsWith('"') && resVal.endsWith('"')) {
        try {
          return JSON.parse(JSON.parse(resVal));
        } catch {
          return JSON.parse(resVal);
        }
      }
      return JSON.parse(resVal);
    }
  } catch {
    // Official CLI not connected or error
  }
  return null;
}

/**
 * Universal dispatcher: Official Unity CLI -> HTTP Bridge -> File Queue
 */
async function sendRequestToUnity(prefix, endpoint, spec, csharpMethod) {
  if (!fs.existsSync(QUEUE_DIR)) fs.mkdirSync(QUEUE_DIR, { recursive: true });
  const cleanName = (spec.name || 'Item').replace(/[^a-zA-Z0-9_-]/g, '_');
  const tempSpecPath = path.join(QUEUE_DIR, `temp_${Date.now()}_${cleanName}.json`).replace(/\\/g, '/');
  fs.writeFileSync(tempSpecPath, JSON.stringify(spec, null, 2), 'utf-8');

  // 1. Try official Unity CLI eval
  if (csharpMethod) {
    const evalCsharp = `string json = System.IO.File.ReadAllText("${tempSpecPath}"); return ${csharpMethod}(json);`;
    const cliRes = tryOfficialUnityCommand(evalCsharp);
    if (cliRes && cliRes.success) {
      try { fs.unlinkSync(tempSpecPath); } catch {}
      return cliRes;
    }
  }

  // 2. Try HTTP Bridge
  const serverUp = await isServerRunning();
  if (serverUp) {
    try {
      const res = await httpRequest(
        {
          hostname: '127.0.0.1',
          port: DEFAULT_PORT,
          path: endpoint,
          method: 'POST',
          headers: { 'Content-Type': 'application/json' }
        },
        spec
      );
      try { fs.unlinkSync(tempSpecPath); } catch {}
      return res.body;
    } catch {}
  }

  // 3. Fallback: File Queue
  if (!fs.existsSync(FINISHED_DIR)) fs.mkdirSync(FINISHED_DIR, { recursive: true });

  const timestamp = Date.now();
  const queueFile = path.join(QUEUE_DIR, `${prefix}_${timestamp}_${cleanName}.json`);
  const resultFile = path.join(FINISHED_DIR, `${prefix}_${timestamp}_${cleanName}_result.json`);

  fs.writeFileSync(queueFile, JSON.stringify(spec, null, 2), 'utf-8');
  try { fs.unlinkSync(tempSpecPath); } catch {}

  console.log(`[UI CLI] Enqueued payload: ${queueFile}`);
  console.log('[UI CLI] Waiting for Unity Editor to process queue...');

  const start = Date.now();
  while (Date.now() - start < 4000) {
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
    message: 'Payload written to queue. Unity Editor will process it on next update.'
  };
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
  const result = await sendRequestToUnity(
    'build',
    '/api/build',
    spec,
    'UIQualityLife.Editor.UIComponentBuilder.BuildFromJsonString'
  );
  console.log('[UI CLI] Result:', JSON.stringify(result, null, 2));
}

async function handleTheme(args) {
  let themeSpec = {
    name: args.name || 'CustomTheme',
    description: args.description || 'Theme created from web resources.',
    colorTokens: [],
    floatTokens: []
  };

  if (args.spec) {
    const specPath = path.resolve(process.cwd(), args.spec);
    themeSpec = JSON.parse(fs.readFileSync(specPath, 'utf-8'));
  } else if (args.json) {
    themeSpec = JSON.parse(args.json);
  } else {
    const rawInput = args.code || args.colors || '';
    const parsed = parseCssThemeTokens(rawInput);
    themeSpec.colorTokens = parsed.colorTokens;
    themeSpec.floatTokens = parsed.floatTokens;
  }

  if (args.name) themeSpec.name = args.name;
  if (args.description) themeSpec.description = args.description;

  console.log(`[UI CLI] Building Theme Palette: '${themeSpec.name}' (${themeSpec.colorTokens?.length || 0} colors)...`);
  const result = await sendRequestToUnity(
    'theme',
    '/api/build-theme',
    themeSpec,
    'UIQualityLife.Editor.UIStylingBuilder.BuildThemeFromJson'
  );
  console.log('[UI CLI] Theme Result:', JSON.stringify(result, null, 2));
}

async function handleAnimation(args) {
  let animSpec = null;

  if (args.spec) {
    const specPath = path.resolve(process.cwd(), args.spec);
    animSpec = JSON.parse(fs.readFileSync(specPath, 'utf-8'));
  } else if (args.json) {
    animSpec = JSON.parse(args.json);
  } else {
    const rawCode = args.code || '';
    animSpec = parseCssAnimation(rawCode, args.name || 'CustomAnimation');
    if (args.states) {
      try {
        animSpec.states = JSON.parse(args.states);
      } catch {}
    }
  }

  if (args.name) animSpec.name = args.name;
  if (args.class || args.className) animSpec.className = args.class || args.className;
  if (args.loop) animSpec.loopType = args.loop;
  if (args.speed) animSpec.loopSpeed = parseFloat(args.speed);
  if (args.magnitude) animSpec.loopMagnitude = parseFloat(args.magnitude);
  if (args.bounce !== undefined) animSpec.enableClickBounce = args.bounce === 'true' || args.bounce === true;

  console.log(`[UI CLI] Building Animation Class: '${animSpec.name}'...`);
  const result = await sendRequestToUnity(
    'anim',
    '/api/build-animation',
    animSpec,
    'UIQualityLife.Editor.UIStylingBuilder.BuildAnimationFromJson'
  );
  console.log('[UI CLI] Animation Result:', JSON.stringify(result, null, 2));
}

async function handleStyle(args) {
  let styleSpec = {
    name: args.name || 'CustomStyle',
    className: args.class || args.className || `.${(args.name || 'custom').toLowerCase()}`,
    description: args.description || 'Compound style class.',
    colorToken: args['theme-token'] || args.colorToken || 'primary',
    borderColorToken: args['border-token'] || args.borderColorToken || 'border',
    radiusToken: args['radius-token'] || args.radiusToken || 'radius-md',
    animationClassName: args['anim-class'] || args.animationClassName || ''
  };

  if (args.spec) {
    styleSpec = JSON.parse(fs.readFileSync(path.resolve(process.cwd(), args.spec), 'utf-8'));
  }

  console.log(`[UI CLI] Building Style Class: '${styleSpec.name}'...`);
  const result = await sendRequestToUnity(
    'style',
    '/api/build-style',
    styleSpec,
    'UIQualityLife.Editor.UIStylingBuilder.BuildStyleFromJson'
  );
  console.log('[UI CLI] Style Result:', JSON.stringify(result, null, 2));
}

async function handleSpawn(args) {
  if (!args.name) {
    console.error('[UI CLI Error] Must specify --name <PresetName>');
    process.exit(1);
  }

  const csharp = `return UIQualityLife.Editor.UIComponentBuilder.SpawnPresetByName("${args.name}");`;
  const cliRes = tryOfficialUnityCommand(csharp);
  if (cliRes === true) {
    console.log(`[UI CLI] Successfully spawned '${args.name}' via official Unity Pipeline!`);
    return;
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

async function handleInspect(args) {
  if (!args.name) {
    console.error('[UI CLI Error] Must specify --name <GameObjectName>');
    process.exit(1);
  }

  try {
    const res = await httpRequest({
      hostname: '127.0.0.1',
      port: DEFAULT_PORT,
      path: `/api/inspect?name=${encodeURIComponent(args.name)}`,
      method: 'GET'
    });
    console.log('[UI CLI] In-Scene Component Inspection:');
    console.log(JSON.stringify(res.body, null, 2));
  } catch (err) {
    console.error(`[UI CLI Error] Could not inspect: ${err.message}`);
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
    } else if (!parsed.name) {
      parsed.name = arg;
    }
  }
  return parsed;
}

async function main() {
  const rawArgs = process.argv.slice(2);
  if (rawArgs.length === 0) {
    console.log(`
UI Quality of Life - Component & Styling CLI

Usage:
  UI Components:
    node Scripts/ui-cli.js build --code "<button ...>Click</button>" --name "PrimaryBtn" --category "Buttons"
    node Scripts/ui-cli.js build --spec <path/to/spec.json>
    node Scripts/ui-cli.js spawn --name "PrimaryBtn"
    node Scripts/ui-cli.js list
    node Scripts/ui-cli.js inspect --name "PrimaryBtn"

  Theme Palettes:
    node Scripts/ui-cli.js theme --name "CyberNeon" --code ":root { --primary: #00f0ff; --surface: #0a0a12; }"
    node Scripts/ui-cli.js theme --name "WarmSunset" --colors "primary=#ff5722,surface=#212121,accent=#ffc107"

  CSS Animations:
    node Scripts/ui-cli.js animation --name "CardHover" --code ".card:hover { transform: scale(1.05) translateY(-4px); }"
    node Scripts/ui-cli.js animation --name "PulseBadge" --loop Pulse --speed 1.5

  Style Classes:
    node Scripts/ui-cli.js style --name "Btn_Modern" --theme-token primary --anim-class CardHover

  Bridge Status:
    node Scripts/ui-cli.js status
`);
    return;
  }

  const args = parseArgs(rawArgs);

  switch (args.command) {
    case 'build':
      await handleBuild(args);
      break;
    case 'theme':
      await handleTheme(args);
      break;
    case 'animation':
    case 'anim':
      await handleAnimation(args);
      break;
    case 'style':
      await handleStyle(args);
      break;
    case 'spawn':
      await handleSpawn(args);
      break;
    case 'inspect':
      await handleInspect(args);
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

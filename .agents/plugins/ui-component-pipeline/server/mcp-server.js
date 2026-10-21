#!/usr/bin/env node

/**
 * Antigravity MCP Server for Unity UI Quality of Life Pipeline.
 * Exposes tools over stdio JSON-RPC 2.0 to build, spawn, and inspect UI components.
 */

const readline = require('readline');
const http = require('http');
const fs = require('fs');
const path = require('path');

const PROJECT_ROOT = path.resolve(__dirname, '../../../..');
const SPEC_PARSER = path.join(PROJECT_ROOT, 'Scripts/spec-parser.js');
const QUEUE_DIR = path.join(PROJECT_ROOT, 'Assets/_Project/Pipeline/Queue');
const FINISHED_DIR = path.join(PROJECT_ROOT, 'Assets/_Project/Pipeline/Finished');
const DEFAULT_PORT = 8765;

const { parseWebCodeToSpec } = require(SPEC_PARSER);

function httpRequest(options, postData = null, timeoutMs = 20000) {
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
    req.setTimeout(timeoutMs, () => {
      req.destroy();
      reject(new Error(`Connection timed out after ${timeoutMs}ms.`));
    });

    if (postData) {
      req.write(typeof postData === 'string' ? postData : JSON.stringify(postData));
    }
    req.end();
  });
}

async function isBridgeRunning() {
  try {
    const res = await httpRequest(
      {
        hostname: '127.0.0.1',
        port: DEFAULT_PORT,
        path: '/api/status',
        method: 'GET'
      },
      null,
      2000
    );
    return res.status === 200;
  } catch {
    return false;
  }
}

async function executeBuild(spec) {
  const bridgeOnline = await isBridgeRunning();

  if (bridgeOnline) {
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
  }

  // Fallback: write to file queue
  if (!fs.existsSync(QUEUE_DIR)) fs.mkdirSync(QUEUE_DIR, { recursive: true });
  if (!fs.existsSync(FINISHED_DIR)) fs.mkdirSync(FINISHED_DIR, { recursive: true });

  const timestamp = Date.now();
  const cleanName = (spec.name || 'Component').replace(/[^a-zA-Z0-9_-]/g, '_');
  const queueFile = path.join(QUEUE_DIR, `${timestamp}_${cleanName}.json`);
  const resultFile = path.join(FINISHED_DIR, `${timestamp}_${cleanName}_result.json`);

  fs.writeFileSync(queueFile, JSON.stringify(spec, null, 2), 'utf-8');

  // Wait for result file up to 8 seconds
  const start = Date.now();
  while (Date.now() - start < 8000) {
    if (fs.existsSync(resultFile)) {
      const raw = fs.readFileSync(resultFile, 'utf-8');
      try {
        return JSON.parse(raw);
      } catch {
        return { success: true, message: raw };
      }
    }
    await new Promise((r) => setTimeout(r, 400));
  }

  return {
    success: true,
    message: `Component spec written to queue: ${queueFile}. Unity Editor will compile and bake it on next tick.`
  };
}

async function handleToolCall(name, args) {
  if (name === 'build_ui_component') {
    let spec = null;
    if (args.spec) {
      spec = typeof args.spec === 'string' ? JSON.parse(args.spec) : args.spec;
    } else if (args.code) {
      let rawCode = args.code;
      if (fs.existsSync(rawCode)) {
        rawCode = fs.readFileSync(rawCode, 'utf-8');
      }
      const compName = args.name || 'GeneratedUI';
      const category = args.category || 'General';
      spec = parseWebCodeToSpec(rawCode, compName, [category]);
    } else {
      throw new Error("Must provide either 'code' (HTML/Tailwind) or 'spec' (JSON component specification).");
    }

    if (args.name) spec.name = args.name;
    if (args.category) spec.categories = [args.category];
    if (args.description) spec.description = args.description;

    const result = await executeBuild(spec);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(result, null, 2)
        }
      ]
    };
  }

  if (name === 'list_ui_presets') {
    const bridgeOnline = await isBridgeRunning();
    if (bridgeOnline) {
      const res = await httpRequest({
        hostname: '127.0.0.1',
        port: DEFAULT_PORT,
        path: '/api/presets',
        method: 'GET'
      });
      return {
        content: [{ type: 'text', text: JSON.stringify(res.body, null, 2) }]
      };
    } else {
      // Read directly from disk
      const presetsDir = path.join(PROJECT_ROOT, 'Assets/_Project/Presets');
      const presets = [];
      if (fs.existsSync(presetsDir)) {
        const findAssets = (dir) => {
          const files = fs.readdirSync(dir, { withFileTypes: true });
          for (const f of files) {
            const full = path.join(dir, f.name);
            if (f.isDirectory() && f.name !== 'Thumbnails') {
              findAssets(full);
            } else if (f.isFile() && f.name.endsWith('.asset')) {
              presets.push({
                name: f.name.replace('.asset', ''),
                folder: path.basename(dir)
              });
            }
          }
        };
        findAssets(presetsDir);
      }
      return {
        content: [{ type: 'text', text: JSON.stringify({ presets }, null, 2) }]
      };
    }
  }

  if (name === 'spawn_ui_component') {
    if (!args.name) throw new Error("Argument 'name' is required.");
    const bridgeOnline = await isBridgeRunning();
    if (bridgeOnline) {
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
      return {
        content: [{ type: 'text', text: JSON.stringify(res.body, null, 2) }]
      };
    } else {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              success: false,
              error: 'Unity Editor Bridge is currently offline. Ensure Unity Editor is open.'
            })
          }
        ]
      };
    }
  }

  if (name === 'inspect_ui_component') {
    const bridgeOnline = await isBridgeRunning();
    if (bridgeOnline) {
      const targetName = args.name || '';
      const res = await httpRequest(
        {
          hostname: '127.0.0.1',
          port: DEFAULT_PORT,
          path: `/api/inspect?name=${encodeURIComponent(targetName)}`,
          method: 'GET'
        },
        null,
        10000
      );
      return {
        content: [{ type: 'text', text: JSON.stringify(res.body, null, 2) }]
      };
    } else {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              success: false,
              error: 'Unity Editor Bridge is currently offline. Ensure Unity Editor is open.'
            })
          }
        ]
      };
    }
  }

  throw new Error(`Tool not found: ${name}`);
}

const TOOLS_MANIFEST = [
  {
    name: 'build_ui_component',
    description:
      'Compiles web code (HTML/Tailwind), images, or JSON specs into native Unity UGUI prefabs with auto-framing 256x256 thumbnails and shelf presets.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Name of the component (e.g. "PrimaryButton", "ModernCard", "PriceBadge")'
        },
        category: {
          type: 'string',
          description: 'Category / tag for shelf organization (e.g. "Buttons", "Cards", "Inputs", "HUD")'
        },
        code: {
          type: 'string',
          description: 'Web code snippet (HTML, CSS, or Tailwind markup) to convert into Unity UI'
        },
        spec: {
          type: 'object',
          description: 'Declarative UIComponentSpec JSON object describing the UI hierarchy, layout, and colors'
        },
        description: {
          type: 'string',
          description: 'Optional human-readable description of what this UI component does'
        }
      }
    }
  },
  {
    name: 'list_ui_presets',
    description: 'Lists all available UI presets and categories currently in the Unity project.',
    inputSchema: {
      type: 'object',
      properties: {}
    }
  },
  {
    name: 'spawn_ui_component',
    description: 'Instantiates an existing UI preset directly into the active Canvas in the open Unity scene.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'The name of the preset to spawn (e.g. "Btn_Primary", "InputField")'
        }
      },
      required: ['name']
    }
  },
  {
    name: 'inspect_ui_component',
    description:
      'Inspects an instantiated UI component in the active scene, returning its hierarchy, RectTransform dimensions, layout groups, and text content.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'The name of the GameObject to inspect in the scene (e.g. "ProgressWithLabel", "Btn_Primary")'
        }
      },
      required: ['name']
    }
  }
];

// Setup standard JSON-RPC 2.0 stdio loop
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

rl.on('line', async (line) => {
  if (!line || !line.trim()) return;

  let request;
  try {
    request = JSON.parse(line.trim());
  } catch (err) {
    return;
  }

  const { id, method, params } = request;

  if (method === 'initialize') {
    const response = {
      jsonrpc: '2.0',
      id: id,
      result: {
        protocolVersion: '2024-11-05',
        capabilities: { tools: {} },
        serverInfo: {
          name: 'ui-pipeline-mcp',
          version: '1.0.0'
        }
      }
    };
    process.stdout.write(JSON.stringify(response) + '\n');
    return;
  }

  if (method === 'notifications/initialized') {
    return;
  }

  if (method === 'tools/list') {
    const response = {
      jsonrpc: '2.0',
      id: id,
      result: {
        tools: TOOLS_MANIFEST
      }
    };
    process.stdout.write(JSON.stringify(response) + '\n');
    return;
  }

  if (method === 'tools/call') {
    try {
      const toolName = params?.name;
      const toolArgs = params?.arguments || {};
      const result = await handleToolCall(toolName, toolArgs);

      const response = {
        jsonrpc: '2.0',
        id: id,
        result: result
      };
      process.stdout.write(JSON.stringify(response) + '\n');
    } catch (err) {
      const response = {
        jsonrpc: '2.0',
        id: id,
        error: {
          code: -32603,
          message: err.message
        }
      };
      process.stdout.write(JSON.stringify(response) + '\n');
    }
    return;
  }

  if (id !== undefined) {
    process.stdout.write(
      JSON.stringify({
        jsonrpc: '2.0',
        id: id,
        error: { code: -32601, message: `Method '${method}' not found.` }
      }) + '\n'
    );
  }
});

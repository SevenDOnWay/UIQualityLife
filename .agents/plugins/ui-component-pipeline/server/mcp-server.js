#!/usr/bin/env node

/**
 * Antigravity MCP Server for Unity UI Quality of Life Pipeline.
 * Exposes tools over stdio JSON-RPC 2.0 to build, spawn, and inspect UI components,
 * Themes/Color Palettes, and CSS Animation Classes.
 */

const readline = require('readline');
const http = require('http');
const fs = require('fs');
const path = require('path');

const PROJECT_ROOT = path.resolve(__dirname, '../../../..');
const SPEC_PARSER = path.join(PROJECT_ROOT, 'Scripts/spec-parser.js');
const STYLING_PARSER = path.join(PROJECT_ROOT, 'Scripts/styling-parser.js');
const QUEUE_DIR = path.join(PROJECT_ROOT, 'Assets/_Project/Pipeline/Queue');
const FINISHED_DIR = path.join(PROJECT_ROOT, 'Assets/_Project/Pipeline/Finished');
const DEFAULT_PORT = 8765;

const { parseWebCodeToSpec } = require(SPEC_PARSER);
const { parseCssThemeTokens, parseCssAnimation } = require(STYLING_PARSER);

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

async function executeBridgePost(endpoint, prefix, payload) {
  const bridgeOnline = await isBridgeRunning();

  if (bridgeOnline) {
    const res = await httpRequest(
      {
        hostname: '127.0.0.1',
        port: DEFAULT_PORT,
        path: endpoint,
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      },
      payload
    );
    return res.body;
  }

  // Fallback: write to file queue
  if (!fs.existsSync(QUEUE_DIR)) fs.mkdirSync(QUEUE_DIR, { recursive: true });
  if (!fs.existsSync(FINISHED_DIR)) fs.mkdirSync(FINISHED_DIR, { recursive: true });

  const timestamp = Date.now();
  const cleanName = (payload.name || 'Item').replace(/[^a-zA-Z0-9_-]/g, '_');
  const queueFile = path.join(QUEUE_DIR, `${prefix}_${timestamp}_${cleanName}.json`);
  const resultFile = path.join(FINISHED_DIR, `${prefix}_${timestamp}_${cleanName}_result.json`);

  fs.writeFileSync(queueFile, JSON.stringify(payload, null, 2), 'utf-8');

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
    message: `Payload written to queue: ${queueFile}. Unity Editor will process it on next tick.`
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

    const result = await executeBridgePost('/api/build', 'build', spec);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(result, null, 2)
        }
      ]
    };
  }

  if (name === 'build_ui_theme') {
    let themeSpec = {
      name: args.name || 'CustomTheme',
      description: args.description || 'Color Palette and Design Tokens',
      colorTokens: [],
      floatTokens: []
    };

    if (args.spec) {
      themeSpec = typeof args.spec === 'string' ? JSON.parse(args.spec) : args.spec;
    } else {
      const rawInput = args.code || args.colors || '';
      const parsed = parseCssThemeTokens(rawInput);
      themeSpec.colorTokens = parsed.colorTokens;
      themeSpec.floatTokens = parsed.floatTokens;
    }

    if (args.name) themeSpec.name = args.name;
    if (args.description) themeSpec.description = args.description;

    const result = await executeBridgePost('/api/build-theme', 'theme', themeSpec);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(result, null, 2)
        }
      ]
    };
  }

  if (name === 'build_ui_animation') {
    let animSpec = null;
    if (args.spec) {
      animSpec = typeof args.spec === 'string' ? JSON.parse(args.spec) : args.spec;
    } else {
      const rawCode = args.code || '';
      animSpec = parseCssAnimation(rawCode, args.name || 'CustomAnimation');
      if (args.states) {
        animSpec.states = typeof args.states === 'string' ? JSON.parse(args.states) : args.states;
      }
    }

    if (args.name) animSpec.name = args.name;
    if (args.className) animSpec.className = args.className;
    if (args.description) animSpec.description = args.description;
    if (args.loopType) animSpec.loopType = args.loopType;
    if (args.loopSpeed) animSpec.loopSpeed = parseFloat(args.loopSpeed);
    if (args.loopMagnitude) animSpec.loopMagnitude = parseFloat(args.loopMagnitude);
    if (args.enableClickBounce !== undefined) animSpec.enableClickBounce = !!args.enableClickBounce;

    const result = await executeBridgePost('/api/build-animation', 'anim', animSpec);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(result, null, 2)
        }
      ]
    };
  }

  if (name === 'build_ui_style_class') {
    let styleSpec = {
      name: args.name || 'CustomStyle',
      className: args.className || `.${(args.name || 'custom').toLowerCase()}`,
      description: args.description || 'Compound style class.',
      colorToken: args.themeToken || args.colorToken || 'primary',
      borderColorToken: args.borderColorToken || 'border',
      radiusToken: args.radiusToken || 'radius-md',
      animationClassName: args.animationClassName || ''
    };

    if (args.spec) {
      styleSpec = typeof args.spec === 'string' ? JSON.parse(args.spec) : args.spec;
    }

    const result = await executeBridgePost('/api/build-style', 'style', styleSpec);
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
    name: 'build_ui_theme',
    description:
      'Compiles CSS variables (:root { --primary: #... }), Tailwind color tokens, or hex color lists into a native UIThemePalette asset with an auto-baked 8x8 dynamic square thumbnail.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'The theme palette name (e.g. "CyberpunkNeon", "OceanBreeze", "AutumnWarmth")'
        },
        code: {
          type: 'string',
          description: 'Raw web snippet containing CSS variables, Tailwind color objects, or hex color codes'
        },
        colors: {
          type: 'string',
          description: 'Comma/space-separated color assignments, e.g. "primary=#3b82f6,surface=#1e2025,accent=#10b981"'
        },
        description: {
          type: 'string',
          description: 'Human-readable description of the theme palette'
        },
        spec: {
          type: 'object',
          description: 'Explicit ThemeBuildSpec JSON with colorTokens and floatTokens arrays'
        }
      },
      required: ['name']
    }
  },
  {
    name: 'build_ui_animation',
    description:
      'Compiles CSS hover/active transitions, @keyframes, or visual states into a native UIAnimationClass asset with thumbnail and tween curves.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Animation class identifier (e.g. "CardHoverBounce", "PulseGlow", "TabSwitch")'
        },
        code: {
          type: 'string',
          description: 'CSS snippet containing hover/active pseudo-classes (.btn:hover { transform: scale(1.05); }), transitions, or @keyframes'
        },
        loopType: {
          type: 'string',
          enum: ['None', 'Pulse', 'Float', 'Shimmer', 'Shake'],
          description: 'Continuous procedural loop type'
        },
        loopSpeed: {
          type: 'number',
          description: 'Speed multiplier for the continuous loop (default 1.5)'
        },
        loopMagnitude: {
          type: 'number',
          description: 'Magnitude of the continuous loop (default 0.05)'
        },
        enableClickBounce: {
          type: 'boolean',
          description: 'Whether to enable procedural spring micro-bounce on click/pointer down'
        },
        states: {
          type: 'array',
          description: 'Custom list of visual states (Normal, Hover, Pressed, Focused, Disabled, etc.)'
        },
        description: {
          type: 'string',
          description: 'Description of the animation profile'
        }
      },
      required: ['name']
    }
  },
  {
    name: 'build_ui_style_class',
    description:
      'Creates a compound UIStyleClass asset that links theme color tokens, border radius tokens, and interactive animation classes.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Style class name (e.g. "Btn_Primary_Style", "Card_Surface_Interactive")'
        },
        themeToken: {
          type: 'string',
          description: 'Primary color token identifier in UIThemePalette (e.g. "primary", "surface", "accent")'
        },
        borderColorToken: {
          type: 'string',
          description: 'Border color token identifier (e.g. "border")'
        },
        radiusToken: {
          type: 'string',
          description: 'Corner radius token identifier (e.g. "radius-md", "radius-full")'
        },
        animationClassName: {
          type: 'string',
          description: 'Name of the UIAnimationClass asset to link for interactive states'
        },
        description: {
          type: 'string',
          description: 'Description of the compound style class'
        }
      },
      required: ['name']
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
      'Inspects an instantiated UI component in the active scene, returning its full hierarchy tree, exact RectTransform dimensions (rect, sizeDelta), z-order layer (siblingIndex, depth), positions (anchoredPosition, anchors, pivot), layout groups, layout elements, and text metrics (wrapping, overflow, preferred dimensions, isVerticalDistorted) for validation and bug fixing.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'The name of the GameObject to inspect in the scene (e.g. "ModalConfirmation", "HUDHealthBar")'
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
          version: '1.1.0'
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
          message: err.message || 'Internal MCP server error'
        }
      };
      process.stdout.write(JSON.stringify(response) + '\n');
    }
  }
});

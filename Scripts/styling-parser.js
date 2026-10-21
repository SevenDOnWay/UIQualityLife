/**
 * Web Snippet Parser for UI Themes and Animation Classes.
 * Extracts CSS variables, Tailwind tokens, hex color lists, CSS transitions,
 * pseudo-classes (:hover, :active, :focus), and @keyframes.
 */

function rgbToHex(r, g, b) {
  const toHex = (n) => {
    const hex = Math.round(Math.max(0, Math.min(255, n))).toString(16);
    return hex.length === 1 ? '0' + hex : hex;
  };
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function normalizeHex(hex) {
  if (!hex) return '#ffffff';
  hex = hex.trim();
  if (hex.startsWith('rgb')) {
    const match = hex.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/i);
    if (match) {
      return rgbToHex(parseInt(match[1]), parseInt(match[2]), parseInt(match[3]));
    }
  }
  if (!hex.startsWith('#')) hex = '#' + hex;
  if (hex.length === 4) {
    // #abc -> #aabbcc
    return `#${hex[1]}${hex[1]}${hex[2]}${hex[2]}${hex[3]}${hex[3]}`;
  }
  return hex;
}

/**
 * Parses CSS variables, Tailwind color configs, or loose hex codes.
 */
function parseCssThemeTokens(rawCode) {
  if (!rawCode || typeof rawCode !== 'string') {
    return { colorTokens: [], floatTokens: [] };
  }

  const colorTokens = [];
  const floatTokens = [];
  const seenColors = new Set();
  const seenFloats = new Set();

  // 1. Match CSS variables: --primary-color: #3b82f6; or --radius: 8px;
  const cssVarRegex = /--([a-zA-Z0-9_-]+)\s*:\s*([^;}\n]+)/g;
  let match;
  while ((match = cssVarRegex.exec(rawCode)) !== null) {
    const id = match[1].trim();
    const val = match[2].trim();

    // Check if color
    if (val.startsWith('#') || val.startsWith('rgb') || val.startsWith('hsl')) {
      const hex = normalizeHex(val);
      if (!seenColors.has(id)) {
        seenColors.add(id);
        colorTokens.push({ id, hex, description: `CSS var --${id}` });
      }
    } else {
      // Check if float (px, rem, etc.)
      const numMatch = val.match(/^([0-9.]+)(px|rem|em)?$/);
      if (numMatch && !seenFloats.has(id)) {
        let num = parseFloat(numMatch[1]);
        if (numMatch[2] === 'rem') num *= 16;
        seenFloats.add(id);
        floatTokens.push({ id, value: num, description: `CSS var --${id}` });
      }
    }
  }

  // 2. Match JS / Tailwind object configs: primary: '#3b82f6', or 'sky-500': '#0ea5e9'
  const jsTokenRegex = /['"]?([a-zA-Z0-9_-]+)['"]?\s*:\s*['"](#[0-9a-fA-F]{3,8}|rgba?\([^)]+\))['"]/g;
  while ((match = jsTokenRegex.exec(rawCode)) !== null) {
    const id = match[1].trim();
    const hex = normalizeHex(match[2].trim());
    if (!seenColors.has(id)) {
      seenColors.add(id);
      colorTokens.push({ id, hex, description: `Token ${id}` });
    }
  }

  // 3. Fallback: match comma or space-separated key-value pairs (e.g. "primary=#3b82f6,surface=#1e2025")
  const kvRegex = /([a-zA-Z0-9_-]+)\s*=\s*(#[0-9a-fA-F]{3,8}|rgba?\([^)]+\))/g;
  while ((match = kvRegex.exec(rawCode)) !== null) {
    const id = match[1].trim();
    const hex = normalizeHex(match[2].trim());
    if (!seenColors.has(id)) {
      seenColors.add(id);
      colorTokens.push({ id, hex, description: `Token ${id}` });
    }
  }

  // 4. Fallback: if no tokens yet, extract raw standalone hex codes
  if (colorTokens.length === 0) {
    const rawHexes = rawCode.match(/#[0-9a-fA-F]{3,8}/g);
    if (rawHexes) {
      const defaultNames = ['primary', 'secondary', 'surface', 'accent', 'background', 'border', 'muted', 'highlight'];
      rawHexes.forEach((rawHex, idx) => {
        const hex = normalizeHex(rawHex);
        const id = idx < defaultNames.length ? defaultNames[idx] : `color_${idx + 1}`;
        if (!seenColors.has(id)) {
          seenColors.add(id);
          colorTokens.push({ id, hex, description: `Color ${idx + 1}` });
        }
      });
    }
  }

  return { colorTokens, floatTokens };
}

/**
 * Parses CSS transitions, pseudo-classes, and @keyframes into AnimationBuildSpec.
 */
function parseCssAnimation(rawCode, defaultName = 'CustomAnimation') {
  const result = {
    name: defaultName,
    className: `.${defaultName.toLowerCase()}`,
    description: 'Extracted CSS Animation and Visual States',
    states: [],
    enableClickBounce: true,
    bounceMagnitude: 0.08,
    bounceDuration: 0.22,
    loopType: 'None',
    loopSpeed: 1.5,
    loopMagnitude: 0.05
  };

  if (!rawCode || typeof rawCode !== 'string') {
    return result;
  }

  // Detect loop behaviors from @keyframes or keywords
  const lower = rawCode.toLowerCase();
  if (lower.includes('@keyframes pulse') || lower.includes('animation: pulse') || lower.includes('animate-pulse')) {
    result.loopType = 'Pulse';
    result.loopMagnitude = 0.06;
  } else if (lower.includes('float') || lower.includes('bobbing') || lower.includes('animate-bounce')) {
    result.loopType = 'Float';
    result.loopMagnitude = 0.08;
  } else if (lower.includes('shimmer') || lower.includes('animate-shimmer')) {
    result.loopType = 'Shimmer';
  } else if (lower.includes('shake') || lower.includes('wiggle') || lower.includes('animate-wiggle')) {
    result.loopType = 'Shake';
  }

  // Check animation duration / speed
  const speedMatch = lower.match(/(?:animation|transition)(?:-duration)?:\s*([0-9.]+)s/);
  if (speedMatch) {
    const dur = parseFloat(speedMatch[1]);
    if (dur > 0) result.loopSpeed = Math.max(0.2, Math.min(5.0, 1.0 / dur));
  }

  // Extract CSS pseudo blocks: :hover, :active / :pressed, :focus, :disabled
  const parseBlock = (pseudo, stateName, defaultScale, defaultOffset) => {
    const regex = new RegExp(`(?::|\\.)${pseudo}[^{]*\\{([^}]+)\\}`, 'i');
    const match = rawCode.match(regex);
    if (!match) return null;

    const block = match[1];
    const state = {
      stateName: stateName,
      overrideScale: false,
      scale: [1, 1, 1],
      overrideOffset: false,
      offset: [0, 0],
      overrideRotation: false,
      rotation: 0,
      overrideColor: false,
      colorMultiplierHex: '#ffffff',
      overrideAlpha: false,
      alpha: 1,
      duration: 0.15,
      ease: 'easeInOut'
    };

    // Scale
    const scaleMatch = block.match(/scale\s*\(\s*([0-9.]+)(?:,\s*([0-9.]+))?\s*\)/i);
    if (scaleMatch) {
      state.overrideScale = true;
      const sX = parseFloat(scaleMatch[1]);
      const sY = scaleMatch[2] ? parseFloat(scaleMatch[2]) : sX;
      state.scale = [sX, sY, 1];
    } else if (defaultScale) {
      state.overrideScale = true;
      state.scale = defaultScale;
    }

    // Offset (translate / translateY / translateX)
    const transYMatch = block.match(/translateY\s*\(\s*(-?[0-9.]+)px\s*\)/i);
    const transXMatch = block.match(/translateX\s*\(\s*(-?[0-9.]+)px\s*\)/i);
    if (transYMatch || transXMatch) {
      state.overrideOffset = true;
      const oX = transXMatch ? parseFloat(transXMatch[1]) : 0;
      // In Unity UI, positive Y is usually up (whereas CSS positive Y is down)
      const oY = transYMatch ? -parseFloat(transYMatch[1]) : 0;
      state.offset = [oX, oY];
    } else if (defaultOffset) {
      state.overrideOffset = true;
      state.offset = defaultOffset;
    }

    // Rotation
    const rotMatch = block.match(/rotate\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
    if (rotMatch) {
      state.overrideRotation = true;
      state.rotation = parseFloat(rotMatch[1]);
    }

    // Opacity
    const opMatch = block.match(/opacity\s*:\s*([0-9.]+)/i);
    if (opMatch) {
      state.overrideAlpha = true;
      state.alpha = parseFloat(opMatch[1]);
    }

    // Transition duration & timing
    const durMatch = block.match(/(?:transition|duration)\s*:\s*([0-9.]+)s/i);
    if (durMatch) {
      state.duration = parseFloat(durMatch[1]);
    }

    if (block.includes('cubic-bezier') || block.includes('spring')) {
      state.ease = 'spring';
    } else if (block.includes('ease-out')) {
      state.ease = 'easeOut';
    } else if (block.includes('linear')) {
      state.ease = 'linear';
    }

    return state;
  };

  const hoverState = parseBlock('hover', 'Hover', [1.05, 1.05, 1], [0, 2]);
  if (hoverState) result.states.push(hoverState);

  const activeState = parseBlock('(?:active|pressed)', 'Pressed', [0.95, 0.95, 1], [0, -2]);
  if (activeState) result.states.push(activeState);

  const focusState = parseBlock('focus', 'Focused', [1.03, 1.03, 1], null);
  if (focusState) result.states.push(focusState);

  const disabledState = parseBlock('disabled', 'Disabled', [0.98, 0.98, 1], null);
  if (disabledState) result.states.push(disabledState);

  // If no explicit pseudo-classes were found but code has transform/scale
  if (result.states.length === 0) {
    const scaleMatch = rawCode.match(/scale\s*\(\s*([0-9.]+)\s*\)/i);
    if (scaleMatch) {
      const s = parseFloat(scaleMatch[1]);
      result.states.push({
        stateName: 'Hover',
        overrideScale: true,
        scale: [s, s, 1],
        overrideOffset: true,
        offset: [0, 2],
        duration: 0.15,
        ease: 'easeInOut'
      });
      result.states.push({
        stateName: 'Pressed',
        overrideScale: true,
        scale: [Math.max(0.8, s - 0.1), Math.max(0.8, s - 0.1), 1],
        overrideOffset: true,
        offset: [0, -2],
        duration: 0.08,
        ease: 'easeInOut'
      });
    }
  }

  return result;
}

module.exports = {
  parseCssThemeTokens,
  parseCssAnimation,
  normalizeHex
};

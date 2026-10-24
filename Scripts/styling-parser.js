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
/**
 * Parses CSS transitions, pseudo-classes, and @keyframes into AnimationBuildSpec
 * supporting atomic channels (Slide, Scale, 3D Rotate, Fade, Punch/Shake, Pivot).
 */
function parseCssAnimation(rawCode, defaultName = 'CustomAnimation') {
  let detectedName = defaultName;
  if (!rawCode || typeof rawCode !== 'string') {
    return {
      name: defaultName,
      className: `.${defaultName.toLowerCase()}`,
      description: 'Atomic Multi-Channel CSS Animation',
      states: [],
      enableClickBounce: true,
      bounceMagnitude: 0.08,
      bounceDuration: 0.22,
      loopType: 'None',
      loopSpeed: 1.5,
      loopMagnitude: 0.05,
      playOnEnable: false,
      enterScaleFrom: 0.85,
      enterFadeFrom: 0.0,
      enterDuration: 0.25
    };
  }

  // Detect keyframe or animation name from comments or keyframes declaration
  const nameMatch = rawCode.match(/(?:@-webkit-keyframes|@keyframes)\s+([a-zA-Z0-9_-]+)/i) ||
                    rawCode.match(/animation\s*:?\s*([a-zA-Z0-9_-]+)/i) ||
                    rawCode.match(/animation\s+([a-zA-Z0-9_-]+)/i);
  if (nameMatch && defaultName === 'CustomAnimation') {
    const rawFound = nameMatch[1].trim();
    detectedName = rawFound
      .split(/[-_]/)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join('');
  }

  const result = {
    name: detectedName,
    className: `.${detectedName.toLowerCase()}`,
    description: `Atomic UI Animation: ${detectedName}`,
    states: [],
    enableClickBounce: true,
    bounceMagnitude: 0.08,
    bounceDuration: 0.22,
    loopType: 'None',
    loopSpeed: 1.5,
    loopMagnitude: 0.05,
    playOnEnable: false,
    enterScaleFrom: 1.0,
    enterFadeFrom: 0.0,
    enterDuration: 0.65,
    slide: null,
    scale: null,
    rotate3d: null,
    fade: null,
    punchShake: null,
    pivot: null
  };

  const lower = rawCode.toLowerCase();

  // 1. Check animation duration / speed
  let parsedDuration = 0.65;
  const speedMatch = lower.match(/(?:animation|transition)(?:-duration)?:\s*([0-9.]+)s/);
  if (speedMatch) {
    parsedDuration = parseFloat(speedMatch[1]);
    if (parsedDuration > 0) result.loopSpeed = Math.max(0.2, Math.min(5.0, 1.0 / parsedDuration));
  }
  result.enterDuration = parsedDuration;

  // 2. Check for Looping properties
  if (lower.includes('infinite') || lower.includes('animation-iteration-count:\s*infinite')) {
    result.loopType = lower.includes('alternate') ? 'Yoyo' : 'Restart';
  }

  // 3. Detect Swing / Pendulum
  if (lower.includes('swing') || lower.includes('pendulum') || lower.includes('wobble')) {
    result.playOnEnable = true;
    result.pivot = [0.5, 1.0]; // Swing from top-center
    result.rotate3d = {
      enabled: true,
      fromEuler: [0, 0, 25],
      toEuler: [0, 0, 0],
      isOscillating: true,
      oscillationCycles: 2.5,
      damping: 1.5
    };
    result.description = `Harmonic Pendulum Swing: Top-pivot oscillating rotation.`;
  }

  // 4. Detect HeadShake / Error Punch
  if (lower.includes('headshake') || lower.includes('shake') && lower.includes('error')) {
    result.playOnEnable = true;
    result.punchShake = {
      enabled: true,
      punchPosition: [16, 0, 0],
      punchScale: [0, 0, 0],
      punchRotation: 0,
      vibrato: 12,
      elasticity: 0.75,
      duration: parsedDuration
    };
    result.description = `Error HeadShake: Additive horizontal vibration.`;
  }

  // 5. Detect RubberBand / Jello (Squash & Stretch)
  if (lower.includes('rubberband') || lower.includes('jello') || lower.includes('tada')) {
    result.playOnEnable = true;
    result.punchShake = {
      enabled: true,
      punchPosition: [0, 0, 0],
      punchScale: [0.25, -0.25, 0], // Volume-preserving squash & stretch
      punchRotation: 0,
      vibrato: 8,
      elasticity: 0.85,
      duration: parsedDuration
    };
    result.description = `Squash & Stretch RubberBand: Additive inverse X/Y volume deformation.`;
  }

  // 6. Detect 3D Card Flips (flipInX, flipInY, card-flip)
  const rotYMatch = rawCode.match(/rotateY\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
  const rotXMatch = rawCode.match(/rotateX\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
  const rot3dMatch = rawCode.match(/rotate3d\s*\(\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*(-?[0-9.]+)deg\s*\)/i);

  if (rotYMatch || rotXMatch || rot3dMatch || lower.includes('flipin') || lower.includes('cardflip')) {
    result.playOnEnable = true;
    let startEuler = [0, 90, 0];
    if (rotYMatch) startEuler = [0, parseFloat(rotYMatch[1]), 0];
    else if (rotXMatch) startEuler = [parseFloat(rotXMatch[1]), 0, 0];
    else if (rot3dMatch) {
      const x = parseFloat(rot3dMatch[1]);
      const y = parseFloat(rot3dMatch[2]);
      const deg = parseFloat(rot3dMatch[4]);
      startEuler = [x * deg, y * deg, 0];
    }

    result.rotate3d = {
      enabled: true,
      fromEuler: startEuler,
      toEuler: [0, 0, 0],
      isOscillating: false
    };
    result.fade = {
      enabled: true,
      fromAlpha: 0.0,
      toAlpha: 1.0
    };
    result.description = `3D Card Flip: Rotates from [${startEuler.join(', ')}]° to resting orientation.`;
  }

  // 7. Detect Slide Keyframes (translate, translateY, translateX, slide-in)
  const transYMatch = rawCode.match(/translateY\s*\(\s*(-?[0-9.]+)px\s*\)/i);
  const transXMatch = rawCode.match(/translateX\s*\(\s*(-?[0-9.]+)px\s*\)/i);
  const transPercY = rawCode.match(/translateY\s*\(\s*(-?[0-9.]+)%\s*\)/i);
  const transPercX = rawCode.match(/translateX\s*\(\s*(-?[0-9.]+)%\s*\)/i);

  if (transYMatch || transXMatch || transPercY || transPercX || lower.includes('slide-in')) {
    result.playOnEnable = true;
    let offsetX = 0;
    let offsetY = 0;
    let isScreenRelative = false;

    if (transXMatch) offsetX = parseFloat(transXMatch[1]);
    if (transYMatch) offsetY = -parseFloat(transYMatch[1]); // Invert for Unity Y-up

    if (transPercX) {
      offsetX = parseFloat(transPercX[1]) > 0 ? 100 : -100;
      isScreenRelative = true;
    }
    if (transPercY) {
      offsetY = parseFloat(transPercY[1]) > 0 ? -100 : 100;
      isScreenRelative = true;
    }

    if (lower.includes('slide-in-bottom')) { offsetY = -150; }
    else if (lower.includes('slide-in-top')) { offsetY = 150; }
    else if (lower.includes('slide-in-left')) { offsetX = -200; }
    else if (lower.includes('slide-in-right')) { offsetX = 200; }

    result.slide = {
      enabled: true,
      direction: 'Custom',
      offset: [offsetX, offsetY],
      useScreenRelative: isScreenRelative
    };
  }

  // 8. Detect Multi-Stage Keyframes & Scale (0%, 50%, 100%)
  const kf0Match = rawCode.match(/(?:0%|from)\s*\{([^}]+)\}/i);
  const kf50Match = rawCode.match(/50%\s*\{([^}]+)\}/i);
  const kf100Match = rawCode.match(/(?:100%|to)\s*\{([^}]+)\}/i);

  if (kf0Match && kf100Match) {
    const b0 = kf0Match[1];
    const b100 = kf100Match[1];

    // Check RotateZ in keyframes (e.g. rotateZ(0) to rotateZ(360deg))
    const rz0Match = b0.match(/rotateZ?\s*\(\s*(-?[0-9.]+)(?:deg)?\s*\)/i);
    const rz100Match = b100.match(/rotateZ?\s*\(\s*(-?[0-9.]+)(?:deg)?\s*\)/i);

    if (rz0Match || rz100Match) {
      const z0 = rz0Match ? parseFloat(rz0Match[1]) : 0;
      const z100 = rz100Match ? parseFloat(rz100Match[1]) : 360;
      result.playOnEnable = true;
      result.rotate3d = {
        enabled: true,
        fromEuler: [0, 0, z0],
        toEuler: [0, 0, z100],
        isOscillating: false
      };
    }

    const s0Match = b0.match(/scale\s*\(\s*([0-9.]+)\s*\)/i);
    const s100Match = b100.match(/scale\s*\(\s*([0-9.]+)\s*\)/i);
    const s50Match = kf50Match ? kf50Match[1].match(/scale\s*\(\s*([0-9.]+)\s*\)/i) : null;

    if (s0Match && s50Match) {
      const s0 = parseFloat(s0Match[1]);
      const s50 = parseFloat(s50Match[1]);
      const s100 = s100Match ? parseFloat(s100Match[1]) : s0;

      if (s50 !== s0) {
        result.playOnEnable = true;
        result.enterScaleFrom = s0;
        result.scale = {
          enabled: true,
          fromScale: [s0, s0, 1.0],
          toScale: [s50, s50, 1.0],
          timing: {
            overrideTiming: true,
            delay: 0,
            duration: parsedDuration,
            ease: 'bell'
          }
        };
        result.description = `Rotate & Scale Pulse: Continuous rotation with intermediate scale peak (${s0}x -> ${s50}x -> ${s100}x).`;
      }
    } else if (s0Match && s100Match) {
      const s0 = parseFloat(s0Match[1]);
      const s100 = parseFloat(s100Match[1]);
      if (s0 !== s100) {
        result.playOnEnable = true;
        result.enterScaleFrom = s0;
        result.scale = {
          enabled: true,
          fromScale: [s0, s0, 1.0],
          toScale: [s100, s100, 1.0]
        };
      }
    }

    const op0Match = b0.match(/opacity\s*:\s*([0-9.]+)/i);
    if (op0Match) {
      result.playOnEnable = true;
      result.enterFadeFrom = parseFloat(op0Match[1]);
      result.fade = {
        enabled: true,
        fromAlpha: parseFloat(op0Match[1]),
        toAlpha: 1.0
      };
    }
  }

  // 9. Detect Decoupled Timing in transitions
  // e.g. "transition: opacity 0.15s ease, transform 0.4s cubic-bezier(...)"
  const opacityTrans = rawCode.match(/opacity\s+([0-9.]+)s/i);
  const transformTrans = rawCode.match(/transform\s+([0-9.]+)s/i);
  if (opacityTrans && transformTrans && result.fade) {
    result.fade.timing = {
      overrideTiming: true,
      delay: 0,
      duration: parseFloat(opacityTrans[1]),
      ease: 'linear'
    };
  }

  // 10. Detect Standard Loops
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

  // 11. Extract CSS pseudo blocks: :hover, :active / :pressed, :focus, :disabled
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
      rotationEuler: [0, 0, 0],
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

    // Offset
    const tY = block.match(/translateY\s*\(\s*(-?[0-9.]+)px\s*\)/i);
    const tX = block.match(/translateX\s*\(\s*(-?[0-9.]+)px\s*\)/i);
    if (tY || tX) {
      state.overrideOffset = true;
      state.offset = [tX ? parseFloat(tX[1]) : 0, tY ? -parseFloat(tY[1]) : 0];
    } else if (defaultOffset) {
      state.overrideOffset = true;
      state.offset = defaultOffset;
    }

    // 3D Rotation
    const rY = block.match(/rotateY\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
    const rX = block.match(/rotateX\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
    const rZ = block.match(/rotate\s*\(\s*(-?[0-9.]+)deg\s*\)/i);
    if (rY || rX || rZ) {
      state.overrideRotation = true;
      const eX = rX ? parseFloat(rX[1]) : 0;
      const eY = rY ? parseFloat(rY[1]) : 0;
      const eZ = rZ ? parseFloat(rZ[1]) : 0;
      state.rotationEuler = [eX, eY, eZ];
      state.rotation = eZ;
    }

    // Opacity
    const opMatch = block.match(/opacity\s*:\s*([0-9.]+)/i);
    if (opMatch) {
      state.overrideAlpha = true;
      state.alpha = parseFloat(opMatch[1]);
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

  // If atomic motion channels are present, also populate Hover / Pressed clips so interactive triggers can call them
  if (result.scale || result.rotate3d || result.slide) {
    const isPressAnimation = lower.includes('down') || lower.includes('press') || lower.includes('active') || lower.includes('shrink');

    result.hoverClip = {
      enabled: true,
      duration: parsedDuration,
      ease: 'outCubic',
      scale: result.scale,
      rotate: result.rotate3d,
      slide: result.slide,
      fade: result.fade
    };

    if (isPressAnimation) {
      result.pressedClip = {
        enabled: true,
        duration: parsedDuration,
        ease: 'outCubic',
        scale: result.scale,
        rotate: result.rotate3d,
        slide: result.slide,
        fade: result.fade
      };
    }
  }

  // If this is a motion entrance / keyframe animation and no pseudo states were given, provide clean default states
  if (result.playOnEnable && result.states.length === 0) {
    result.states.push(
      { stateName: 'Normal', duration: 0.15 },
      { stateName: 'Hover', overrideScale: true, scale: [1.05, 1.05, 1], overrideOffset: true, offset: [0, 2], duration: 0.15, ease: 'easeInOut' },
      { stateName: 'Pressed', overrideScale: true, scale: [0.95, 0.95, 1], overrideOffset: true, offset: [0, -2], duration: 0.08, ease: 'easeInOut' },
      { stateName: 'Focused', overrideScale: true, scale: [1.03, 1.03, 1], duration: 0.12, ease: 'easeInOut' },
      { stateName: 'Disabled', overrideAlpha: true, alpha: 0.45, overrideScale: true, scale: [0.98, 0.98, 1], duration: 0.15, ease: 'easeInOut' }
    );
  }

  return result;
}

module.exports = {
  parseCssThemeTokens,
  parseCssAnimation,
  normalizeHex
};

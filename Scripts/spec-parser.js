/**
 * Converts web UI code (HTML, Tailwind CSS, inline styles) into a structured UIComponentSpec JSON
 * that the Unity UI Pipeline can compile directly into UGUI prefabs and presets.
 */

const TAILWIND_COLORS = {
  'white': '#FFFFFF',
  'black': '#000000',
  'transparent': '#00000000',
  'slate-900': '#0F172A',
  'slate-800': '#1E293B',
  'slate-700': '#334155',
  'slate-600': '#475569',
  'slate-500': '#64748B',
  'slate-400': '#94A3B8',
  'slate-300': '#CBD5E1',
  'slate-200': '#E2E8F0',
  'slate-100': '#F1F5F9',
  'gray-900': '#111827',
  'gray-800': '#1F2937',
  'gray-700': '#374151',
  'gray-600': '#4B5563',
  'gray-500': '#6B7280',
  'gray-400': '#9CA3AF',
  'gray-300': '#D1D5DB',
  'gray-200': '#E5E7EB',
  'gray-100': '#F3F4F6',
  'blue-600': '#2563EB',
  'blue-500': '#3B82F6',
  'blue-700': '#1D4ED8',
  'indigo-600': '#4F46E5',
  'indigo-500': '#6366F1',
  'indigo-700': '#4338CA',
  'purple-600': '#9333EA',
  'purple-500': '#A855F7',
  'emerald-600': '#059669',
  'emerald-500': '#10B981',
  'red-600': '#DC2626',
  'red-500': '#EF4444',
  'red-700': '#B91C1C',
  'amber-500': '#F59E0B',
  'yellow-400': '#FACC15'
};

function parseTailwindColor(token) {
  if (!token) return null;
  // Match arbitrary hex like bg-[#1e2025] or text-[#ffffff]
  const hexMatch = token.match(/\[#(?:[0-9a-fA-F]{3,8})\]/);
  if (hexMatch) {
    return hexMatch[0].replace('[', '').replace(']', '');
  }

  for (const [key, hex] of Object.entries(TAILWIND_COLORS)) {
    if (token.endsWith(key)) return hex;
  }
  return null;
}

/**
 * Parses simple HTML string into a hierarchical UIComponentSpec.
 */
function parseWebCodeToSpec(code, componentName = 'WebComponent', categories = ['Web']) {
  const trimmed = code.trim();

  // Auto-detect React function/component name if generic default was passed
  const funcMatch = trimmed.match(/export\s+(?:default\s+)?(?:function|const)\s+([A-Za-z0-9_]+)/);
  if (funcMatch && (!componentName || componentName === 'WebComponent')) {
    componentName = funcMatch[1];
  }

  const spec = {
    name: componentName,
    categories: Array.isArray(categories) ? categories : [categories],
    description: `Generated from web code snippet: ${componentName}`,
    previewColorHex: '#2563EB',
    root: {
      name: componentName,
      type: 'Container',
      width: 320,
      height: 120,
      backgroundColorHex: '#1E2025',
      layout: {
        type: 'Vertical',
        spacing: 8,
        paddingLeft: 12,
        paddingRight: 12,
        paddingTop: 12,
        paddingBottom: 12,
        childControlWidth: true,
        childControlHeight: false
      },
      children: []
    }
  };

  // 1. Detect Shadcn / React Input Field Groups (<Field>, <InputGroup>, etc.)
  if (/<Field/i.test(trimmed) || /<InputGroup/i.test(trimmed)) {
    spec.categories = ['Inputs', 'Web'];
    spec.root = parseShadcnField(trimmed, componentName);
    spec.previewColorHex = '#3B82F6';
    return spec;
  }

  // 2. Detect if code is a simple Button
  if (/^<button/i.test(trimmed) || /<Button[\s>]/i.test(trimmed)) {
    spec.categories = ['Buttons', 'Web'];
    spec.root = parseButtonElement(trimmed, componentName);
    spec.previewColorHex = spec.root.normalColorHex || '#2563EB';
    return spec;
  }

  // 3. Detect if code is an Input Field
  if (/^<input/i.test(trimmed)) {
    spec.categories = ['Inputs', 'Web'];
    spec.root = parseInputElement(trimmed, componentName);
    spec.previewColorHex = '#3B82F6';
    return spec;
  }

  // 4. Fallback: Parse container with elements (Cards, Dialogs, etc.)
  parseGenericElements(trimmed, spec.root);
  return spec;
}

function parseShadcnField(html, defaultName) {
  // Label extraction
  const labelMatch = html.match(/<FieldLabel[^>]*>([\s\S]*?)<\/FieldLabel>/i);
  const labelText = labelMatch ? labelMatch[1].replace(/<[^>]*>/g, '').trim() : 'Input';

  // Placeholder and Input details
  const phMatch = html.match(/placeholder=["']([^"']+)["']/i);
  const placeholder = phMatch ? phMatch[1] : 'Enter value...';

  // Description / helper text
  const descMatch = html.match(/<FieldDescription[^>]*>([\s\S]*?)<\/FieldDescription>/i);
  const descText = descMatch ? descMatch[1].replace(/<[^>]*>/g, '').trim() : '';

  // Icon detection
  let iconGlyph = '👁';
  if (/EyeOff/i.test(html)) iconGlyph = '👁';
  else if (/Search/i.test(html)) iconGlyph = '🔍';
  else if (/Lock/i.test(html)) iconGlyph = '🔒';
  else if (/Mail/i.test(html)) iconGlyph = '✉';
  else if (/Check/i.test(html)) iconGlyph = '✓';

  const rootChildren = [];

  // 1. Label
  if (labelText) {
    rootChildren.push({
      name: 'FieldLabel',
      type: 'Text',
      text: labelText,
      fontSize: 13,
      fontStyle: 'Bold',
      textColorHex: '#F4F4F5',
      height: 20,
      alignment: 'Left'
    });
  }

  // 2. Input Group (Container with Input + Icon Addon)
  const groupChildren = [
    {
      name: 'InputGroupInput',
      type: 'InputField',
      width: 250,
      height: 34,
      placeholderText: placeholder,
      textColorHex: '#FFFFFF',
      placeholderColorHex: '#71717A',
      backgroundColorHex: '#18181B',
      fontSize: 13
    }
  ];

  if (/<InputGroupAddon/i.test(html) || /Icon/i.test(html)) {
    groupChildren.push({
      name: 'InputGroupAddon',
      type: 'Button',
      width: 32,
      height: 32,
      text: iconGlyph,
      fontSize: 15,
      normalColorHex: '#3F3F46',
      highlightColorHex: '#52525B',
      pressedColorHex: '#27272A',
      textColorHex: '#E4E4E7'
    });
  }

  rootChildren.push({
    name: 'InputGroup',
    type: 'Container',
    height: 42,
    backgroundColorHex: '#27272A',
    layout: {
      type: 'Horizontal',
      spacing: 8,
      paddingLeft: 8,
      paddingRight: 8,
      paddingTop: 4,
      paddingBottom: 4,
      childControlWidth: false,
      childControlHeight: true
    },
    children: groupChildren
  });

  // 3. Description
  if (descText) {
    rootChildren.push({
      name: 'FieldDescription',
      type: 'Text',
      text: descText,
      fontSize: 11,
      textColorHex: '#71717A',
      height: 18,
      alignment: 'Left'
    });
  }

  let totalHeight = 24;
  for (const c of rootChildren) {
    totalHeight += (c.height || 30) + 6;
  }

  return {
    name: defaultName,
    type: 'Container',
    width: 320,
    height: Math.max(90, totalHeight),
    backgroundColorHex: '#18181B',
    layout: {
      type: 'Vertical',
      spacing: 6,
      paddingLeft: 12,
      paddingRight: 12,
      paddingTop: 10,
      paddingBottom: 10,
      childControlWidth: true,
      childControlHeight: false
    },
    children: rootChildren
  };
}

function parseButtonElement(html, defaultName) {
  const textMatch = html.match(/>([^<]+)<\/button>/i);
  const text = textMatch ? textMatch[1].trim() : 'Button';

  const classMatch = html.match(/class=["']([^"']+)["']/i);
  const classes = classMatch ? classMatch[1].split(/\s+/) : [];

  let normalColor = '#2563EB';
  let highlightColor = '#3B82F6';
  let pressedColor = '#1D4ED8';
  let textColor = '#FFFFFF';
  let fontSize = 14;
  let fontStyle = 'Normal';
  let width = 160;
  let height = 44;

  for (const cls of classes) {
    if (cls.startsWith('bg-')) {
      const c = parseTailwindColor(cls);
      if (c) normalColor = c;
    } else if (cls.startsWith('hover:bg-')) {
      const c = parseTailwindColor(cls);
      if (c) highlightColor = c;
    } else if (cls.startsWith('active:bg-')) {
      const c = parseTailwindColor(cls);
      if (c) pressedColor = c;
    } else if (cls.startsWith('text-')) {
      if (cls === 'text-xs') fontSize = 11;
      else if (cls === 'text-sm') fontSize = 13;
      else if (cls === 'text-base') fontSize = 15;
      else if (cls === 'text-lg') fontSize = 17;
      else if (cls === 'text-xl') fontSize = 20;
      else {
        const c = parseTailwindColor(cls);
        if (c) textColor = c;
      }
    } else if (cls === 'font-bold' || cls === 'font-semibold') {
      fontStyle = 'Bold';
    } else if (cls === 'font-italic') {
      fontStyle = 'Italic';
    } else if (cls.startsWith('w-')) {
      const pxMatch = cls.match(/w-\[(\d+)px\]/);
      if (pxMatch) width = parseInt(pxMatch[1], 10);
    } else if (cls.startsWith('h-')) {
      const pxMatch = cls.match(/h-\[(\d+)px\]/);
      if (pxMatch) height = parseInt(pxMatch[1], 10);
    }
  }

  return {
    name: defaultName,
    type: 'Button',
    width: width,
    height: height,
    text: text,
    fontSize: fontSize,
    fontStyle: fontStyle,
    normalColorHex: normalColor,
    highlightColorHex: highlightColor,
    pressedColorHex: pressedColor,
    textColorHex: textColor
  };
}

function parseInputElement(html, defaultName) {
  const phMatch = html.match(/placeholder=["']([^"']+)["']/i);
  const placeholder = phMatch ? phMatch[1] : 'Enter text...';

  return {
    name: defaultName,
    type: 'InputField',
    width: 200,
    height: 44,
    placeholderText: placeholder,
    backgroundColorHex: '#1E2025',
    textColorHex: '#FFFFFF',
    placeholderColorHex: '#64748B',
    fontSize: 14
  };
}

function parseGenericElements(html, rootNode) {
  // Extract headers
  const hMatches = html.matchAll(/<h[1-6][^>]*>([^<]+)<\/h[1-6]>/gi);
  for (const m of hMatches) {
    rootNode.children.push({
      name: 'Heading',
      type: 'Text',
      text: m[1].trim(),
      fontSize: 16,
      fontStyle: 'Bold',
      textColorHex: '#FFFFFF',
      height: 24,
      alignment: 'Left'
    });
  }

  // Extract paragraphs
  const pMatches = html.matchAll(/<p[^>]*>([^<]+)<\/p>/gi);
  for (const m of pMatches) {
    rootNode.children.push({
      name: 'Paragraph',
      type: 'Text',
      text: m[1].trim(),
      fontSize: 12,
      textColorHex: '#94A3B8',
      height: 36,
      wrap: true,
      alignment: 'Left'
    });
  }

  // Extract buttons inside
  const btnMatches = html.matchAll(/<button[^>]*>([^<]+)<\/button>/gi);
  for (const m of btnMatches) {
    rootNode.children.push({
      name: 'Button',
      type: 'Button',
      text: m[1].trim(),
      fontSize: 13,
      fontStyle: 'Bold',
      normalColorHex: '#2563EB',
      highlightColorHex: '#3B82F6',
      pressedColorHex: '#1D4ED8',
      textColorHex: '#FFFFFF',
      height: 38
    });
  }

  if (rootNode.children.length === 0) {
    // If no specific HTML matched, add a placeholder text block
    rootNode.children.push({
      name: 'ContentText',
      type: 'Text',
      text: html.replace(/<[^>]*>/g, '').trim() || 'UI Component',
      fontSize: 14,
      textColorHex: '#FFFFFF',
      height: 40
    });
  }

  // Adjust card height based on children
  let totalH = 32;
  for (const c of rootNode.children) {
    totalH += (c.height || 40) + 8;
  }
  rootNode.height = Math.max(120, totalH);
  rootNode.width = 300;
}

module.exports = {
  parseWebCodeToSpec
};

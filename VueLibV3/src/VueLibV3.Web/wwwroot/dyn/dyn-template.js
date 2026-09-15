/* ============================================================
 * VueLibV3  dyn-template.js  模板导入/导出
 * ------------------------------------------------------------
 * 导入：粘贴 ElementPlus Vue Template 片段 → 解析成 DynCom JSON
 *       （使用 DOMParser 解析，递归映射标签/属性/v-model/@事件/插槽）
 * 导出：DynCom JSON → Vue Template 字符串（代码导出工具，运行时不使用模板编译）
 * 边界：复杂表达式/指令保存为 __expr / extendinfo，导入后可在设计器二次微调。
 * ============================================================ */
(function (global) {
  'use strict';

  /* ---------- 导入：Vue Template → DynCom JSON ---------- */

  function importTemplate(html) {
    var doc = new DOMParser().parseFromString(html, 'text/html');
    var nodes = Array.prototype.slice.call(doc.body ? doc.body.children : []);
    return nodes.map(parseEl).filter(Boolean);
  }

  function parseEl(el) {
    if (!el || el.nodeType !== 1) return null;
    var tag = el.tagName.toLowerCase();
    if (tag === 'template') return null; // 顶层 template 由调用方处理
    var component = toPascal(tag);
    var cfg = baseCfg(component);

    Array.prototype.slice.call(el.attributes).forEach(function (a) {
      var name = a.name;
      var value = a.value;

      if (name === 'v-model') { cfg.modelname = value; return; }
      if (name.indexOf('v-model:') === 0) { cfg.modelname = value; cfg.extendinfo.modelModifier = name.replace('v-model:', ''); return; }
      if (name.charAt(0) === '@' || name.indexOf('v-on:') === 0) {
        var evt = name.charAt(0) === '@' ? name.slice(1) : name.slice(5);
        cfg.options.comlisteners[evt] = value;
        return;
      }
      if (name === 'class') { cfg.options.itemoptions.class = value; return; }
      if (name === 'style') { cfg.options.itemoptions.style = parseStyle(value); return; }
      if (name.charAt(0) === ':') {
        cfg.options.comoptions[name.slice(1)] = { __expr: value };
        return;
      }
      if (name.indexOf('v-bind:') === 0) {
        cfg.options.comoptions[name.slice(7)] = { __expr: value };
        return;
      }
      if (name === 'v-if') { cfg.extendinfo.vif = value; return; }
      if (name === 'v-for') { cfg.extendinfo.vfor = value; return; }
      cfg.options.comoptions[name] = parseValue(value);
    });

    // 子节点：template → 具名插槽；其余 → childrenctrls
    Array.prototype.slice.call(el.children).forEach(function (child) {
      var ctag = child.tagName.toLowerCase();
      if (ctag === 'template') {
        var slotName = getSlotName(child);
        var slotChildren = Array.prototype.slice.call(child.children)
          .map(parseEl).filter(Boolean);
        cfg.slots[slotName] = (cfg.slots[slotName] || []).concat(slotChildren);
      } else {
        var sub = parseEl(child);
        if (sub) cfg.childrenctrls.push(sub);
      }
    });

    return cfg;
  }

  function getSlotName(tplEl) {
    var names = Array.prototype.slice.call(tplEl.attributes).map(function (a) { return a.name; });
    for (var i = 0; i < names.length; i++) {
      var n = names[i];
      if (n.charAt(0) === '#') return n.slice(1);
      if (n.indexOf('v-slot:') === 0) return n.slice(7);
      if (n === 'slot') return tplEl.getAttribute('slot');
    }
    return 'default';
  }

  /* ---------- 导出：DynCom JSON → Vue Template ---------- */

  function exportTemplate(cfg, indent) {
    indent = indent || 0;
    var pad = new Array(indent + 1).join('  ');
    var padIn = new Array(indent + 2).join('  ');
    if (!cfg || !cfg.component) return '';
    var lines = [];
    var open = '<' + cfg.component;
    var comoptions = (cfg.options && cfg.options.comoptions) || {};
    var listeners = (cfg.options && cfg.options.comlisteners) || {};
    var itemoptions = (cfg.options && cfg.options.itemoptions) || {};

    Object.keys(comoptions).forEach(function (k) {
      var v = comoptions[k];
      if (v && typeof v === 'object' && '__expr' in v) {
        open += ' :' + k + '="' + v.__expr + '"';
      } else if (typeof v === 'boolean') {
        if (v) open += ' ' + k;
      } else if (v !== '' && v !== undefined && v !== null) {
        open += ' ' + k + '="' + String(v).replace(/"/g, '&quot;') + '"';
      }
    });
    if (cfg.modelname) open += ' v-model="' + cfg.modelname + '"';
    if (itemoptions.class) open += ' class="' + itemoptions.class + '"';
    if (itemoptions.style && Object.keys(itemoptions.style).length) {
      open += ' style="' + styleToString(itemoptions.style) + '"';
    }
    Object.keys(listeners).forEach(function (evt) {
      open += ' @' + evt + '="' + listeners[evt] + '"';
    });

    var children = (cfg.childrenctrls || []).map(function (c) { return exportTemplate(c, indent + 1); });
    Object.keys(cfg.slots || {}).forEach(function (slotName) {
      (cfg.slots[slotName] || []).forEach(function (c) {
        var inner = exportTemplate(c, indent + 2);
        children.push(padIn + '<template #' + slotName + '>');
        children.push(inner);
        children.push(padIn + '</template>');
      });
    });

    if (!children.length) {
      lines.push(pad + open + ' />');
    } else {
      lines.push(pad + open + '>');
      children.forEach(function (c) { if (c) lines.push(c); });
      lines.push(pad + '</' + cfg.component + '>');
    }
    return lines.join('\n');
  }

  /* ---------- 辅助 ---------- */

  function baseCfg(component) {
    return {
      component: component,
      modelname: '',
      options: {
        comoptions: {},
        comlisteners: {},
        labeloptions: { label: '', required: false, show: true },
        itemoptions: { style: {}, class: '' }
      },
      validators: [],
      childrenctrls: [],
      slots: {},
      extendinfo: {}
    };
  }

  function toPascal(tag) {
    return tag.split('-').map(function (s) {
      return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
    }).join('');
  }

  function parseValue(v) {
    if (v === 'true') return true;
    if (v === 'false') return false;
    if (/^-?\d+$/.test(v)) return Number(v);
    if (/^-?\d+\.\d+$/.test(v)) return Number(v);
    return v;
  }

  function parseStyle(str) {
    var ret = {};
    String(str || '').split(';').forEach(function (item) {
      var idx = item.indexOf(':');
      if (idx < 0) return;
      var k = item.slice(0, idx).trim();
      var v = item.slice(idx + 1).trim();
      if (k && v) ret[k] = v;
    });
    return ret;
  }

  function styleToString(style) {
    return Object.keys(style).map(function (k) { return k + ': ' + style[k] + ';'; }).join(' ');
  }

  global.DynTemplate = {
    importTemplate: importTemplate,
    exportTemplate: exportTemplate,
    parseStyle: parseStyle
  };
})(window);

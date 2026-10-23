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
var __plugin = {
  name:'template', stage:'designer', requires:[],
  setup:function(ctx){
  'use strict';

  /* ---------- 导入：Vue Template → DynCom JSON ---------- */

  // Element Plus 标签 → Dyn 组件名映射（同时支持 el-form 和 elform 两种写法）
  var TAG_MAP = {
    'el-form':'DynGridContainer','elform':'DynGridContainer',
    'el-input':'DynElInput','elinput':'DynElInput',
    'el-input-number':'DynElInputNumber','elinputnumber':'DynElInputNumber',
    'el-textarea':'DynElTextarea','eltextarea':'DynElTextarea',
    'el-select':'DynElSelect','elselect':'DynElSelect',
    'el-select-v2':'DynElSelect','elselectv2':'DynElSelect',
    'el-radio-group':'DynElRadioGroup','elradiogroup':'DynElRadioGroup',
    'el-checkbox-group':'DynElCheckboxGroup','elcheckboxgroup':'DynElCheckboxGroup',
    'el-button':'DynElButton','elbutton':'DynElButton',
    'el-date-picker':'DynElDatePicker','eldatepicker':'DynElDatePicker',
    'el-time-picker':'DynElTimePicker','eltimepicker':'DynElTimePicker',
    'el-switch':'DynElSwitch','elswitch':'DynElSwitch',
    'el-segmented':'DynElRadioGroup','elsegmented':'DynElRadioGroup',
    'el-slider':'DynElSlider','elslider':'DynElSlider',
    'el-rate':'DynElRate','elrate':'DynElRate',
    'el-color-picker':'DynElColorPicker','elcolorpicker':'DynElColorPicker',
    'el-cascader':'DynElCascader','elcascader':'DynElCascader',
    'el-transfer':'DynElTransfer','eltransfer':'DynElTransfer',
    'el-table':'DynElTable','eltable':'DynElTable',
    'el-table-column':'DynTableColumn','eltablecolumn':'DynTableColumn',
    'el-tabs':'DynElTabs','eltabs':'DynElTabs',
    'el-collapse':'DynElCollapse','elcollapse':'DynElCollapse',
    'el-collapse-item':'DynElCollapseItem','elcollapseitem':'DynElCollapseItem',
    'el-container':'DynElContainer','elcontainer':'DynElContainer',
    'el-row':'DynElRow','elrow':'DynElRow',
    'el-col':'DynElCol','elcol':'DynElCol',
    'el-card':'DynElCard','elcard':'DynElCard',
    'el-tag':'DynElTag','eltag':'DynElTag',
    'el-alert':'DynElAlert','elalert':'DynElAlert',
    'el-progress':'DynElProgress','elprogress':'DynElProgress',
    'el-empty':'DynElEmpty','elempty':'DynElEmpty',
    'el-image':'DynElImage','elimage':'DynElImage',
    'el-divider':'DynElDivider','eldivider':'DynElDivider',
    'el-popover':'DynElPopover','elpopover':'DynElPopover',
    'el-steps':'DynElSteps','elsteps':'DynElSteps',
    'el-timeline':'DynElTimeline','eltimeline':'DynElTimeline',
    'el-statistic':'DynElStatistic','elstatistic':'DynElStatistic',
    'el-descriptions':'DynElDescriptions','eldescriptions':'DynElDescriptions',
    'el-upload':'DynElUpload','elupload':'DynElUpload'
  };
  // 反向映射：Dyn 组件名 → Element 标签（优先用连字符写法 el-form），供 exportTemplate 往返导出
  var REVERSE_TAG_MAP = {};
  Object.keys(TAG_MAP).forEach(function(k){
    var v = TAG_MAP[k];
    if (!REVERSE_TAG_MAP[v] || k.indexOf('-') !== -1) REVERSE_TAG_MAP[v] = k;
  });

  function importTemplate(html) {
    var doc = new DOMParser().parseFromString(html, 'text/html');
    var nodes = Array.prototype.slice.call(doc.body ? doc.body.children : []);
    return nodes.map(parseEl).filter(Boolean);
  }

  function parseEl(el) {
    if (!el || el.nodeType !== 1) return null;
    var tag = el.tagName.toLowerCase();
    if (tag === 'template') return null; // 顶层 template 由调用方处理

    // el-form-item 不生成独立组件：把 label/required/prop 提取出来塞给第一个子组件
    if (tag === 'el-form-item' || tag === 'elformitem') {
      var label = el.getAttribute('label') || '';
      var required = el.hasAttribute('required');
      var prop = el.getAttribute('prop') || '';
      var childEls = [];
      for (var i = 0; i < el.children.length; i++) {
        var ch = el.children[i];
        if (ch.nodeType === 1 && ch.tagName.toLowerCase() !== 'template') childEls.push(ch);
      }
      if (!childEls.length) return null;
      var subs = childEls.map(parseEl).filter(Boolean);
      if (subs.length && label) subs[0].options.labeloptions.label = label;
      if (subs.length && required) subs[0].options.labeloptions.required = true;
      // prop 或 required 表示需要校验：加 required 校验规则
      if (subs.length && (prop || required)) {
        subs[0].validators = subs[0].validators || [];
        subs[0].validators.push({ type: 'required', message: '此项为必填' });
      }
      if (subs.length === 1) return subs[0];
      var wrap = baseCfg('DynGridContainer');
      wrap.childrenctrls = subs;
      return wrap;
    }

    var component = TAG_MAP[tag] || toPascal(tag);
    var cfg = baseCfg(component);
    // 选项类子节点标签 & 选项类父组件
    var OPTION_TAGS = { 'el-option': 1, 'eloption': 1, 'el-radio': 1, 'elradio': 1, 'el-radio-button': 1, 'elradiobutton': 1, 'el-checkbox': 1, 'elcheckbox': 1, 'el-checkbox-button': 1, 'elcheckboxbutton': 1, 'el-cascader-panel': 1 };
    var optionComps = { 'DynElSelect': 1, 'DynElRadioGroup': 1, 'DynElCheckboxGroup': 1, 'DynElCascader': 1, 'DynElTransfer': 1 };

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
    var hasElementChild = false;
    Array.prototype.slice.call(el.children).forEach(function (child) {
      var ctag = child.tagName.toLowerCase();
      if (ctag === 'template') {
        hasElementChild = true;
        var slotName = getSlotName(child);
        var slotChildren = Array.prototype.slice.call(child.children)
          .map(parseEl).filter(Boolean);
        cfg.slots[slotName] = (cfg.slots[slotName] || []).concat(slotChildren);
      } else {
        hasElementChild = true;
        var sub = parseEl(child);
        if (sub) cfg.childrenctrls.push(sub);
      }
    });
    // 无子元素时，捕获直接文本内容：按钮文字、选项 label 等
    if (!hasElementChild) {
      var txt = (el.textContent || '').trim();
      if (txt) {
        cfg.extendinfo.text = txt;
        // 非选项类组件把文本写入 comoptions.text（Button/Tag 等组件读取此字段显示）
        var lowerComp = component.toLowerCase();
        if (!OPTION_TAGS[lowerComp] && !OPTION_TAGS[component]) {
          cfg.options.comoptions.text = txt;
        }
      }
    }

    // 选项类组件：把 el-option / el-radio / el-checkbox 子节点转为 optionValuesText（每行"值,标签"）
    if (optionComps[component] && cfg.childrenctrls.length) {
      var optLines = [];
      var hasButton = false;
      var hasBorder = false;
      var BUTTON_OPTION = { 'el-radio-button': 1, 'elradiobutton': 1, 'el-checkbox-button': 1, 'elcheckboxbutton': 1 };
      cfg.childrenctrls.forEach(function (c) {
        var lower = c.component.toLowerCase();
        if (OPTION_TAGS[lower] || OPTION_TAGS[c.component]) {
          if (BUTTON_OPTION[lower] || BUTTON_OPTION[c.component]) hasButton = true;
          var co = c.options.comoptions;
          if (co.border === true || co.border === '') hasBorder = true;
          // value: el-option 用 value 属性，el-radio/el-checkbox 用 label 属性
          var val = co.value !== undefined ? co.value : (co.label !== undefined ? co.label : '');
          // label: el-option 优先 label 属性，el-radio/el-checkbox 优先文本内容
          var lab = (c.extendinfo && c.extendinfo.text) ? c.extendinfo.text
            : (co.label !== undefined ? co.label : val);
          if (val !== '' && val !== undefined) {
            optLines.push(String(val) + ',' + String(lab));
          }
        }
      });
      if (optLines.length) {
        cfg.options.comoptions.sourceType = 'static';
        cfg.options.comoptions.optionValuesText = optLines.join('\n');
        // 子节点是 button 类型时，父组设 type="button"
        if (hasButton) cfg.options.comoptions.type = 'button';
        // 任一子节点带 border 时，父组设 border
        if (hasBorder && !hasButton) cfg.options.comoptions.border = true;
        cfg.childrenctrls = cfg.childrenctrls.filter(function (c) {
          var lower = c.component.toLowerCase();
          return !OPTION_TAGS[lower] && !OPTION_TAGS[c.component];
        });
      }
    }

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
    var tag = REVERSE_TAG_MAP[cfg.component] || cfg.component;
    var lines = [];
    var open = '<' + tag;
    var comoptions = (cfg.options && cfg.options.comoptions) || {};
    var listeners = (cfg.options && cfg.options.comlisteners) || {};
    var itemoptions = (cfg.options && cfg.options.itemoptions) || {};
    var labeloptions = (cfg.options && cfg.options.labeloptions) || {};

    // el-form-item 的 label/required 已提到父级，导出时跳过；text 作为标签内容输出
    // radio-group/checkbox-group 的 type/border 是选项子节点属性，不输出到 group 标签
    var isOptGroup = cfg.component === 'DynElRadioGroup' || cfg.component === 'DynElCheckboxGroup';
    Object.keys(comoptions).forEach(function (k) {
      if (k === 'sourceType' || k === 'optionValues' || k === 'optionValuesText' || k === 'text') return;
      if (isOptGroup && (k === 'type' || k === 'border')) return;
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

    // 静态选项还原为子节点：select→el-option, radio-group→el-radio(el-radio-button), checkbox-group→el-checkbox(el-checkbox-button)
    var ovText = comoptions.optionValuesText;
    if (typeof ovText === 'string' && ovText.trim()) {
      var isBtn = comoptions.type === 'button';
      var hasBorder = comoptions.border === true;
      var optTag = 'el-option';
      if (cfg.component === 'DynElRadioGroup') optTag = isBtn ? 'el-radio-button' : 'el-radio';
      else if (cfg.component === 'DynElCheckboxGroup') optTag = isBtn ? 'el-checkbox-button' : 'el-checkbox';
      ovText.split(/\r?\n/).forEach(function (line) {
        line = line.trim();
        if (!line) return;
        var idx = line.indexOf(',');
        var val = idx === -1 ? line : line.slice(0, idx).trim();
        var lab = idx === -1 ? line : line.slice(idx + 1).trim();
        if (optTag === 'el-option') {
          children.push(padIn + '<el-option label="' + lab.replace(/"/g, '&quot;') + '" value="' + val.replace(/"/g, '&quot;') + '" />');
        } else {
          var borderAttr = (!isBtn && hasBorder) ? ' border' : '';
          children.push(padIn + '<' + optTag + ' label="' + val.replace(/"/g, '&quot;') + '"' + borderAttr + '>' + lab + '</' + optTag + '>');
        }
      });
    }

    Object.keys(cfg.slots || {}).forEach(function (slotName) {
      (cfg.slots[slotName] || []).forEach(function (c) {
        var inner = exportTemplate(c, indent + 2);
        children.push(padIn + '<template #' + slotName + '>');
        children.push(inner);
        children.push(padIn + '</template>');
      });
    });

    // 按钮文字等文本内容：comoptions.text 作为标签内容输出
    var btnText = comoptions.text;
    if (typeof btnText === 'string' && btnText && !children.length) {
      lines.push(pad + open + '>' + btnText + '</' + tag + '>');
      return lines.join('\n');
    }

    if (!children.length) {
      lines.push(pad + open + ' />');
    } else {
      lines.push(pad + open + '>');
      children.forEach(function (c) { if (c) lines.push(c); });
      lines.push(pad + '</' + tag + '>');
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
  }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);

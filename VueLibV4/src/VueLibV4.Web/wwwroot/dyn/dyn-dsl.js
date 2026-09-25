/* dyn-dsl.js 类Markdown DSL：解析器(DSL→JSON) / 反向导出(JSON→DSL) / CodeMirror自动提示
 * 语法（与设计器组件契约一致，全小写键名）：
 *   >> 组件名 [属性...]      容器节点（可含缩进子节点）
 *   - 组件名 [属性...]       普通组件
 *   缩进（推荐2空格）        表达 childrenctrls
 *   属性：
 *     modelname="字段名"
 *     comoptions.xxx=值       → options.comoptions.xxx
 *     labeloptions.xxx=值     → options.labeloptions.xxx
 *     label="文本"            → options.labeloptions.label（简写）
 *     required=true           → options.labeloptions.required（简写）
 *   值类型：字符串"..."、布尔true/false、数字、枚举（不加引号）
 *   // 注释行
 * 生成JSON不含 __uid（由 DynDynamicCom 运行时生成）
 */
(function (global) {
  'use strict';

  /* ==================== 组件名简写别名 ==================== */
  const ALIASES = {
    input: 'DynElInput',
    text: 'DynText',
    textarea: 'DynElTextarea',
    number: 'DynElInputNumber',
    password: 'DynElPassword',
    select: 'DynElSelect',
    radio: 'DynElRadioGroup',
    checkbox: 'DynElCheckboxGroup',
    switch: 'DynElSwitch',
    date: 'DynElDatePicker',
    time: 'DynElTimePicker',
    slider: 'DynElSlider',
    rate: 'DynElRate',
    color: 'DynElColorPicker',
    button: 'DynElButton',
    alert: 'DynElAlert',
    tag: 'DynElTag',
    divider: 'DynElDivider',
    image: 'DynElImage',
    progress: 'DynElProgress',
    container: 'DynElContainer',
    grid: 'DynGridContainer',
    card: 'DynElCard',
    tabs: 'DynElTabs',
    collapse: 'DynElCollapse',
    form: 'DynForm',
    table: 'DynTable',
    crud: 'DynCrudPage'
  };

  /* ==================== DSL → JSON 解析 ==================== */
  function emptyNode(componentName) {
    return {
      component: componentName || '',
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

  function parseValue(raw) {
    if (raw == null) return undefined;
    const s = String(raw).trim();
    if (s.length >= 2 && s[0] === '"' && s[s.length - 1] === '"') {
      // 引号字符串支持转义：\\ → \，\n → 换行（下拉选项用真实换行拆分），\t → 制表，\" → 引号
      return s.slice(1, -1)
        .replace(/\\\\/g, '\\')
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t')
        .replace(/\\"/g, '"');
    }
    if (s.length >= 2 && s[0] === "'" && s[s.length - 1] === "'") return s.slice(1, -1);
    if (s === 'true') return true;
    if (s === 'false') return false;
    if (/^-?\d+$/.test(s)) return parseInt(s, 10);
    if (/^-?\d+(\.\d+)?$/.test(s)) return parseFloat(s);
    return s;
  }

  /**
   * 解析一行属性串：key=value 以空白分隔（value 可含空格，双引号包裹）
   * 支持 comoptions.xxx / labeloptions.xxx / modelname / label / required
   */
  function parseAttrs(attrText, node, errors, lineNo) {
    if (!attrText || !attrText.trim()) return;
    // 匹配 key=value：value 优先匹配引号串，否则到下一个空白/属性边界
    const re = /([\w.]+)=("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|[^\s]+)/g;
    let m;
    while ((m = re.exec(attrText))) {
      const key = m[1];
      const rawVal = m[2];
      const val = parseValue(rawVal);
      applyAttr(node, key, val, errors, lineNo);
    }
  }

  // itemoptions.style 赋值：支持 JSON 字符串或简单 CSS（如 "gap:16px"）→ 解析为对象
  function setItemStyle(node, val) {
    if (val == null) return;
    const s = String(val).trim();
    if (s.startsWith('{')) {
      try { node.options.itemoptions.style = JSON.parse(s); return; } catch (e) {}
      // 兼容单引号/裸键 JSON：{width:'50%',padding:'8px'} 或 {'width':'50%'} → 合法 JSON
      try {
        const fixed = s
          .replace(/([{,]\s*)([A-Za-z_$][\w$-]*)\s*:/g, '$1"$2":')  // 裸键加引号
          .replace(/'/g, '"');                                       // 单引号值 → 双引号
        node.options.itemoptions.style = JSON.parse(fixed);
        return;
      } catch (e2) {}
    }
    const obj = {};
    s.split(/[;；]/).forEach(function (seg) {
      const kv = seg.split(/:/);
      if (kv.length >= 2) obj[kv[0].trim()] = kv.slice(1).join(':').trim();
    });
    node.options.itemoptions.style = Object.keys(obj).length ? obj : {};
  }

  function applyAttr(node, key, val, errors, lineNo) {
    if (!key) return;
    if (key === 'modelname') { node.modelname = val == null ? '' : String(val); return; }
    if (key === 'label') { node.options.labeloptions.label = val == null ? '' : String(val); return; }
    if (key === 'required') { node.options.labeloptions.required = !!val; return; }
    // 简写 class=/style= → itemoptions（布局 class/style）
    if (key === 'class') { node.options.itemoptions.class = val == null ? '' : String(val); return; }
    if (key === 'style') { setItemStyle(node, val); return; }
    if (key.startsWith('comoptions.')) {
      const p = key.slice('comoptions.'.length);
      node.options.comoptions[p] = val;
      return;
    }
    if (key.startsWith('labeloptions.')) {
      const p = key.slice('labeloptions.'.length);
      node.options.labeloptions[p] = val;
      return;
    }
    if (key.startsWith('itemoptions.')) {
      const p = key.slice('itemoptions.'.length);
      if (p === 'style' && typeof val === 'string') { setItemStyle(node, val); return; }
      node.options.itemoptions[p] = val;
      return;
    }
    if (errors) errors.push({ line: lineNo, msg: '未知属性: ' + key });
  }

  function resolveComponentName(name, metaIndex) {
    if (!name) return { name: '', isContainer: false };
    const real = ALIASES[name] || name;
    const meta = metaIndex && metaIndex[real];
    return { name: real, isContainer: !!(meta && (meta.IsContainer || meta.isContainer)) };
  }

  /**
   * DSL 文本 → 组件JSON树（根=DynElContainer）
   * @param {string} text
   * @param {object} [metaIndex] ComponentMeta 索引（用于容器识别/组件名校验）
   * @returns {{cfg:object, errors:Array<{line:number,msg:string}>, warnings:Array}}
   */
  function parseDsl(text, metaIndex) {
    const lines = String(text || '').split(/\r?\n/);
    const root = emptyNode('DynElContainer');
    const stack = [{ level: -1, node: root, container: true }];
    const errors = [];
    const warnings = [];

    lines.forEach(function (raw, idx) {
      const lineNo = idx + 1;
      const line = raw.replace(/\s+$/, '');
      const trimmed = line.trim();
      if (!trimmed || /^\/\//.test(trimmed)) return; // 空行/注释
      if (/^#{1,6}\s/.test(trimmed)) {
        warnings.push({ line: lineNo, msg: 'Markdown标题已忽略，请使用 >> 声明容器' });
        return;
      }
      const indent = line.length - line.replace(/^\s+/, '').length;
      const m = trimmed.match(/^(>>|-)\s+([A-Za-z_][\w]*)\s*([\s\S]*)$/);
      if (!m) {
        // 属性续行：带缩进且形如 key=value 的行，追加到最近声明的节点（支持多行属性写法）
        const top = stack[stack.length - 1];
        if (stack.length > 1 && top && top.node && indent > 0 && /=/.test(trimmed)) {
          parseAttrs(trimmed, top.node, errors, lineNo);
          return;
        }
        errors.push({ line: lineNo, msg: '无法识别的行，需以 >> 或 - 开头（如: - DynElInput label="姓名"）' });
        return;
      }
      const isContainerDecl = m[1] === '>>';
      const compName = m[2];
      const attrText = m[3] || '';
      const resolved = resolveComponentName(compName, metaIndex);
      // 注册判定：元数据存在 或 内置别名（无元数据时别名算注册）
      const registered = metaIndex ? !!metaIndex[resolved.name] : (ALIASES[compName] !== undefined);
      if (!registered) {
        // 组件名未注册：无属性 → 视为输入中，跳过（不挂树，避免预览闪现"组件未配置"红字）；有属性 → 报错
        if (!attrText.trim()) {
          warnings.push({ line: lineNo, msg: '组件名输入中或未注册: ' + compName + '（自动提示可补全）' });
          return;
        }
        errors.push({ line: lineNo, msg: '未知组件: ' + compName });
        return;
      }
      const node = emptyNode(resolved.name);
      const isContainerNode = isContainerDecl || resolved.isContainer;

      // 缩进找父：弹出缩进 >= 当前行的栈顶
      while (stack.length > 1 && stack[stack.length - 1].level >= indent) stack.pop();
      let parent = stack[stack.length - 1];
      // 父是非容器（用 - 声明但被识别为容器，或 - 声明普通组件却有人挂子节点）→ 警告并挂到上级
      if (!parent.container) {
        warnings.push({ line: lineNo, msg: '组件 ' + parent.node.component + ' 非容器，其子节点已上挂一层' });
        stack.pop();
        parent = stack[stack.length - 1];
      }
      parseAttrs(attrText, node, errors, lineNo);

      if (!parent.node.childrenctrls) parent.node.childrenctrls = [];
      parent.node.childrenctrls.push(node);

      stack.push({ level: indent, node: node, container: isContainerNode });
    });

    // 根优化：若顶层只有一个容器节点，直接提升为根（避免多余包裹层）
    if (root.childrenctrls && root.childrenctrls.length === 1) {
      const only = root.childrenctrls[0];
      if (only.component !== 'DynElContainer' && only.childrenctrls && only.childrenctrls.length) {
        // 把唯一顶层容器提升为根
        const promoted = only;
        root.component = promoted.component;
        root.options = promoted.options;
        root.childrenctrls = promoted.childrenctrls;
      }
    }

    return { cfg: root, errors: errors, warnings: warnings };
  }

  /* ==================== JSON → DSL 反向导出 ==================== */
  function isDefault(val) {
    return val === undefined || val === null || val === '' || (typeof val === 'object' && Object.keys(val).length === 0);
  }

  function formatValue(v) {
    if (typeof v === 'string') return '"' + v.replace(/"/g, '\\"') + '"';
    if (typeof v === 'boolean') return v ? 'true' : 'false';
    if (typeof v === 'number') return String(v);
    if (v == null) return '""';
    return '"' + String(v).replace(/"/g, '\\"') + '"';
  }

  /**
   * 组件JSON树 → DSL 文本（跳过默认/空值，容器>> 普通-，childrenctrls 递归缩进）
   * @param {object} cfg
   * @param {object} [opts] { skipDefaults:true }
   * @returns {string}
   */
  function toDsl(cfg, opts) {
    opts = opts || {};
    const lines = [];
    function walk(node, depth) {
      if (!node || !node.component) return;
      const indent = '  '.repeat(depth);
      const isCont = !!(node.childrenctrls && node.childrenctrls.length) ||
        /^(DynElContainer|DynGridContainer|DynForm|DynElCard|DynElTabs|DynElCollapse|DynCrudPage|DynTable)$/.test(node.component);
      const head = (isCont ? '>> ' : '- ') + node.component;
      const parts = [];
      if (node.modelname) parts.push('modelname=' + formatValue(node.modelname));
      const lo = (node.options && node.options.labeloptions) || {};
      if (lo.label) parts.push('label=' + formatValue(lo.label));
      if (lo.required === true) parts.push('required=true');
      ['labelposition', 'labelwidth', 'labelPosition', 'labelWidth'].forEach(function (k) {
        if (lo[k] !== undefined && lo[k] !== '') parts.push('labeloptions.' + k + '=' + formatValue(lo[k]));
      });
      const co = (node.options && node.options.comoptions) || {};
      Object.keys(co).forEach(function (k) {
        if (isDefault(co[k])) return;
        if (typeof co[k] === 'object') return; // 复杂对象暂不导出
        parts.push('comoptions.' + k + '=' + formatValue(co[k]));
      });
      lines.push(indent + head + (parts.length ? ' ' + parts.join(' ') : ''));
      (node.childrenctrls || []).forEach(function (child) { walk(child, depth + 1); });
    }
    walk(cfg, 0);
    return lines.join('\n');
  }

  /* ==================== 元数据与提示索引 ==================== */
  let _metaPromise = null;
  let _hintIndex = null;

  function loadMeta() {
    if (_metaPromise) return _metaPromise;
    _metaPromise = fetch('/api/platform/componentmeta/all')
      .then(function (r) { return r.json(); })
      .then(function (res) {
        const list = (res && res.code === 0 && res.data) || [];
        _hintIndex = buildHintIndex(list);
        return _hintIndex;
      })
      .catch(function (e) {
        console.error('[DynDsl] 元数据加载失败', e);
        _hintIndex = buildHintIndex([]);
        return _hintIndex;
      });
    return _metaPromise;
  }

  function buildHintIndex(metas) {
    const byName = {};
    const propGroups = {}; // componentName -> { comoptions:[{key,label,enum}], labeloptions:[...] }
    (metas || []).forEach(function (m) {
      const name = m.ComponentName || m.componentName;
      if (!name) return;
      byName[name] = m;
      propGroups[name] = extractProps(m);
    });
    // 补充别名
    Object.keys(ALIASES).forEach(function (a) {
      const real = ALIASES[a];
      if (!byName[real]) return;
      if (!byName[a]) {
        byName[a] = byName[real];
        propGroups[a] = propGroups[real];
      }
    });
    const components = Object.keys(byName).map(function (name) {
      const m = byName[name];
      return {
        name: name,
        label: m.Label || name,
        category: m.Category || (m.UiPlatform || 'Common'),
        isContainer: !!(m.IsContainer || m.isContainer),
        icon: m.Icon || '🧩'
      };
    }).sort(function (a, b) { return a.name.localeCompare(b.name); });
    return { byName: byName, propGroups: propGroups, components: components, metas: metas };
  }

  /** 从 PropertyConfigJson 提取 comoptions / labeloptions 属性及枚举 */
  function extractProps(meta) {
    const res = { comoptions: [], labeloptions: [] };
    let tree = null;
    try {
      tree = meta.PropertyConfigJson ? (typeof meta.PropertyConfigJson === 'string' ? JSON.parse(meta.PropertyConfigJson) : meta.PropertyConfigJson) : null;
    } catch (e) { tree = null; }
    if (!tree || !tree.childrenctrls) {
      // 回退：DefaultConfigJson 的 comoptions keys
      try {
        const d = meta.DefaultConfigJson ? (typeof meta.DefaultConfigJson === 'string' ? JSON.parse(meta.DefaultConfigJson) : meta.DefaultConfigJson) : null;
        if (d && d.options && d.options.comoptions) {
          Object.keys(d.options.comoptions).forEach(function (k) {
            res.comoptions.push({ key: k, label: k, enum: undefined });
          });
        }
      } catch (e) {}
      return res;
    }
    (tree.childrenctrls || []).forEach(function (child) {
      if (!child || !child.modelname) return;
      const mn = child.modelname;
      const lab = (child.options && child.options.labeloptions && child.options.labeloptions.label) || '';
      let enumValues;
      const childCo = (child.options && child.options.comoptions) || {};
      if (Array.isArray(childCo.optionValues)) {
        enumValues = childCo.optionValues.map(function (o) {
          return (o && typeof o === 'object') ? o.value : o;
        });
      } else if (childCo.optionValuesText && typeof childCo.optionValuesText === 'string') {
        // "radio,圆点单选\nbutton,按钮单选" → ['radio','button']（兼容 JSON 解析出的真实换行 / 字面 \n）
        enumValues = childCo.optionValuesText
          .split(/\r?\n|\\n/)
          .map(function (line) { return String(line).split(',')[0].trim(); })
          .filter(Boolean);
      } else if (child.component === 'DynElSwitch') {
        enumValues = ['true', 'false'];
      }
      if (mn.indexOf('options.comoptions.') === 0) {
        const key = mn.slice('options.comoptions.'.length);
        if (!res.comoptions.some(function (x) { return x.key === key; })) {
          res.comoptions.push({ key: key, label: lab || key, enum: enumValues });
        }
      } else if (mn.indexOf('options.labeloptions.') === 0) {
        const key = mn.slice('options.labeloptions.'.length);
        if (!res.labeloptions.some(function (x) { return x.key === key; })) {
          res.labeloptions.push({ key: key, label: lab || key, enum: enumValues });
        }
      }
    });
    return res;
  }

  function getHintIndex() { return _hintIndex; }

  /* ==================== CodeMirror 自动提示 ==================== */
  /**
   * 给 CodeMirror 实例挂 DSL 自动提示（组件名 / comoptions. / labeloptions. / 枚举值）
   * @param {object} cm CodeMirror 实例
   * @param {object} hintIndex 由 buildHintIndex 生成
   * @returns {void}
   */
  function attachDslHints(cm, hintIndex) {
    if (!cm || !hintIndex) return;
    let hintBox = null;
    let items = [];
    let active = 0;
    let currentApply = null;

    function closeHint() {
      if (hintBox) {
        hintBox.remove();
        hintBox = null;
      }
      items = [];
      active = 0;
      currentApply = null;
    }

    function showHint(list, apply) {
      closeHint();
      if (!list || !list.length) return;
      items = list;
      active = 0;
      currentApply = apply;
      const cur = cm.getCursor();
      let coords;
      try { coords = cm.charCoords({ line: cur.line, ch: cur.ch }, 'page'); }
      catch (e) { coords = { left: 0, bottom: 0 }; }
      const box = document.createElement('div');
      box.className = 'dyn-dsl-hint';
      // z-index 必须高于 layui layer 弹窗（默认19891014），否则提示框被弹窗盖住
      box.style.cssText = 'position:absolute;z-index:2147483000;top:' + (coords.bottom + 4) + 'px;left:' + (coords.left) + 'px;' +
        'min-width:180px;max-width:340px;max-height:220px;overflow:auto;background:#fff;border:1px solid #dcdfe6;' +
        'border-radius:4px;box-shadow:0 4px 12px rgba(0,0,0,.12);font-size:12px;line-height:1.6;';
      list.forEach(function (item, i) {
        const row = document.createElement('div');
        row.className = 'dyn-dsl-hint-item';
        row.style.cssText = 'padding:4px 10px;cursor:pointer;display:flex;gap:8px;align-items:center;' + (i === 0 ? 'background:#ecf5ff;' : '');
        const label = document.createElement('span');
        label.textContent = item.label;
        label.style.cssText = 'font-weight:600;color:#303133;';
        const desc = document.createElement('span');
        desc.textContent = item.detail || '';
        desc.style.cssText = 'color:#909399;font-size:11px;flex:1;text-align:right;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;';
        row.appendChild(label);
        row.appendChild(desc);
        row.addEventListener('mousedown', function (e) {
          e.preventDefault();
          e.stopPropagation();
          closeHint();
          apply(item);
        });
        box.appendChild(row);
      });
      document.body.appendChild(box);
      hintBox = box;
      renderActive();
      // 滚动到可见
      if (box.children[0]) box.children[0].scrollIntoView({ block: 'nearest' });
    }

    function renderActive() {
      if (!hintBox) return;
      const rows = hintBox.children;
      for (let j = 0; j < rows.length; j++) {
        rows[j].style.background = j === active ? '#ecf5ff' : 'transparent';
      }
      const curRow = rows[active];
      if (curRow) curRow.scrollIntoView({ block: 'nearest' });
    }

    function acceptCurrent() {
      if (!hintBox || !items.length) return false;
      const item = items[active];
      const apply = currentApply;
      closeHint();
      if (apply) apply(item);
      cm.focus();
      return true;
    }

    // 用 CodeMirror5 的 extraKeys（keymap 栈顶优先于默认），避免与 CM 内部 keydown 竞争
    cm.setOption('extraKeys', {
      'Enter': function () {
        if (!hintBox) return false; // 无提示：正常换行
        e_stopHintKey(arguments);
        return acceptCurrent();
      },
      'Tab': function () {
        if (!hintBox) return false; // 无提示：正常缩进
        e_stopHintKey(arguments);
        return acceptCurrent();
      },
      'Down': function () {
        if (!hintBox) return false;
        active = Math.min(items.length - 1, active + 1);
        renderActive();
        return true;
      },
      'Up': function () {
        if (!hintBox) return false;
        active = Math.max(0, active - 1);
        renderActive();
        return true;
      },
      'Esc': function () {
        if (!hintBox) return false;
        closeHint();
        return true;
      },
      'Ctrl-Space': function () {
        tryComplete(true);
        return true;
      }
    });
    function e_stopHintKey(args) {
      // CM5 extraKeys 处理器已默认阻止，此辅助仅为兼容性兜底
    }

    // labeloptions 平台标准键（无论组件 PCJ 是否配置都提示，DSL 简写 label=/required= 等价）
    var LABELOPTIONS_STD = [
      { key: 'label', label: '标签文本', enum: undefined },
      { key: 'required', label: '必填', enum: ['true', 'false'] },
      { key: 'show', label: '显示标签', enum: ['true', 'false'] },
      { key: 'labelposition', label: '标签位置', enum: ['left', 'right', 'top'] },
      { key: 'labelwidth', label: '标签宽度', enum: undefined }
    ];

    function tryComplete(manual) {
      closeHint();
      const cur = cm.getCursor();
      const lineText = cm.getLine(cur.line) || '';
      const before = lineText.slice(0, cur.ch);
      // 上下文1：行首 >> 或 - 后补全组件名（含别名）
      const mHead = before.match(/^\s*(>>|-)\s+([\w]*)$/);
      if (mHead && mHead[2] !== undefined) {
        const prefix = mHead[2] || '';
        const items = hintIndex.components
          .filter(function (c) { return c.name.toLowerCase().indexOf(prefix.toLowerCase()) === 0; })
          .slice(0, 30)
          .map(function (c) {
            return { label: c.name, detail: (c.icon || '🧩') + ' ' + (c.category || '') };
          });
        if (items.length) {
          const fromCh = before.length - prefix.length;
          showHint(items, function (item) {
            cm.replaceRange(item.label + ' ', { line: cur.line, ch: fromCh }, { line: cur.line, ch: before.length });
            cm.focus();
          });
        }
        return;
      }
      // 解析当前行组件名（用于属性提示）。支持缩进续行：本行无 >> / - 时向上找最近组件行
      const lineCompMatch = lineText.match(/^\s*(>>|-)\s+([\w]+)/);
      let compName = lineCompMatch ? (ALIASES[lineCompMatch[2]] || lineCompMatch[2]) : null;
      if (!compName) {
        for (let i = cur.line - 1; i >= 0; i--) {
          const m = cm.getLine(i).match(/^\s*(>>|-)\s+([\w]+)/);
          if (m) { compName = ALIASES[m[2]] || m[2]; break; }
        }
      }
      const group = compName ? hintIndex.propGroups[compName] : null;

      // 上下文2：comoptions. / labeloptions. / itemoptions. 补全属性key
      const mProp = before.match(/(comoptions|labeloptions|itemoptions)\.([\w]*)$/);
      if (mProp && group) {
        const gKey = mProp[1];
        const prefix = mProp[2] || '';
        // itemoptions 固定 class/style；labeloptions 平台标准键 + PCJ 提取合并；comoptions 取自元数据索引
        let list;
        if (gKey === 'itemoptions') {
          list = [{ key: 'class', label: '自定义类名(如 grid-span-2)' }, { key: 'style', label: '内联样式(JSON或CSS)' }];
        } else if (gKey === 'labeloptions') {
          list = LABELOPTIONS_STD
            .concat(group[gKey] || [])
            .filter(function (p, i, arr) { return arr.findIndex(function (q) { return q.key === p.key; }) === i; });
        } else {
          list = (group[gKey] || []);
        }
        list = list.filter(function (p) {
          return p.key.toLowerCase().indexOf(prefix.toLowerCase()) === 0;
        }).slice(0, 30);
        const items = list.map(function (p) {
          return { label: p.key, detail: p.label || '' };
        });
        if (items.length) {
          const fromCh = before.length - prefix.length;
          showHint(items, function (item) {
            cm.replaceRange(item.label + '=', { line: cur.line, ch: fromCh }, { line: cur.line, ch: before.length });
            cm.focus();
          });
        }
        return;
      }

      // 上下文3：属性= 后补全枚举值
      const mEnum = before.match(/(comoptions|labeloptions)\.([\w]+)=\s*([\w]*)$/);
      if (mEnum && group) {
        const gKey = mEnum[1];
        const propKey = mEnum[2];
        const prefix = mEnum[3] || '';
        const prop = (gKey === 'labeloptions'
          ? LABELOPTIONS_STD.concat(group[gKey] || []).find(function (p) { return p.key === propKey; })
          : (group[gKey] || []).find(function (p) { return p.key === propKey; }));
        if (prop && Array.isArray(prop.enum) && prop.enum.length) {
          const items = prop.enum.filter(function (v) {
            return String(v).toLowerCase().indexOf(prefix.toLowerCase()) === 0;
          }).slice(0, 30).map(function (v) {
            return { label: String(v), detail: '' };
          });
          if (items.length) {
            const fromCh = before.length - prefix.length;
            showHint(items, function (item) {
              cm.replaceRange(item.label + ' ', { line: cur.line, ch: fromCh }, { line: cur.line, ch: before.length });
              cm.focus();
            });
          }
        }
        return;
      }

      // 上下文4：组件行上输入属性 key → 提示 modelname/comoptions./labeloptions./itemoptions./label/required/class/style
      const topAttrs = [
        { label: 'modelname', detail: '绑定数据字段' },
        { label: 'comoptions.', detail: '组件原生属性' },
        { label: 'labeloptions.', detail: '标签配置(label/required/show/labelwidth...)' },
        { label: 'itemoptions.', detail: '布局 class/style' },
        { label: 'label', detail: '简写=标签文本' },
        { label: 'required', detail: '简写=是否必填' },
        { label: 'class', detail: '简写→itemoptions.class' },
        { label: 'style', detail: '简写→itemoptions.style(JSON或CSS)' }
      ];
      if (compName && !/^\s*$/.test(before) && !/=/.test(before.slice(-4))) {
        const mTop = /(?:^|\s)([\w]*)$/.exec(before);
        const prefix = (mTop && mTop[1]) || '';
        const items = topAttrs.filter(function (a) {
          return a.label.toLowerCase().indexOf(prefix.toLowerCase()) === 0;
        }).slice(0, 15);
        if (items.length) {
          const fromCh = before.length - prefix.length;
          showHint(items, function (item) {
            cm.replaceRange(item.label, { line: cur.line, ch: fromCh }, { line: cur.line, ch: before.length });
            cm.focus();
          });
        }
        return;
      }

      // 手动唤起（Ctrl+Space）且不在任何上下文 → 兜底：弹组件列表（空行/任意行均可）
      if (manual) {
        if (!/^\s*\/\//.test(lineText)) {
          const items = hintIndex.components.slice(0, 40).map(function (c) {
            return { label: c.name, detail: (c.icon || '🧩') + ' ' + (c.category || '') };
          });
          if (items.length) {
            const fromCh = before.length;
            // 前一个字符不是空白时补一个空格，避免 "-alert" 粘连
            const pad = fromCh > 0 && !/\s$/.test(before) ? ' ' : '';
            showHint(items, function (item) {
              cm.replaceRange(pad + item.label + ' ', { line: cur.line, ch: fromCh }, { line: cur.line, ch: before.length });
              cm.focus();
            });
          }
        }
      }
    }

    // 注意：CM5 事件回调首参是 cm 实例，必须包一层，避免把 cm 当作 manual(true) 走兜底
    cm.on('inputRead', function () { tryComplete(false); });
    cm.on('cursorActivity', function () { tryComplete(false); });
    cm.on('blur', closeHint);
    // 注意：键盘绑定（Enter/Tab/方向键/Escape/Ctrl-Space）已在 attachDslHints 顶部
    // 通过 cm.setOption('extraKeys', {...}) 一次性注册，这里不再重复（重复会覆盖）
  }

  /* ==================== Lint 错误标记（CodeMirror5 markText + CSS 波浪线） ==================== */
  /**
   * 解析结果 → 编辑器对应行打波浪线标记（错误红/警告黄）
   * @param {object} cm CodeMirror 实例
   * @param {Array<{line:number,msg:string}>} errors
   * @param {Array<{line:number,msg:string}>} [warnings]
   * @returns {void}
   */
  function markDslIssues(cm, errors, warnings) {
    if (!cm) return;
    // 清除上一次标记（mark 会随文本编辑移动，必须整体重建）
    if (cm._dslIssueMarks) {
      cm._dslIssueMarks.forEach(function (m) { m.clear(); });
      cm._dslIssueMarks = [];
    }
    const doc = cm.getDoc();
    const lineCount = doc.lineCount();
    function apply(issues, className) {
      if (!issues || !issues.length) return;
      issues.forEach(function (issue) {
        const lineNo = (issue.line || 1) - 1; // CodeMirror 0-based
        if (lineNo < 0 || lineNo >= lineCount) return;
        const len = doc.getLine(lineNo).length;
        const mark = doc.markText(
          { line: lineNo, ch: 0 },
          { line: lineNo, ch: Math.max(1, len) },
          { className: className }
        );
        cm._dslIssueMarks.push(mark);
      });
    }
    cm._dslIssueMarks = [];
    apply(errors, 'dyn-dsl-lint-error');
    apply(warnings, 'dyn-dsl-lint-warn');
  }

  /* ==================== CodeMirror DSL 高亮 mode ==================== */
  function defineDslMode(CodeMirror) {
    if (!CodeMirror || CodeMirror.modes && CodeMirror.modes.dsl) return;
    CodeMirror.defineMode('dsl', function () {
      return {
        startState: function () { return { inString: false, inComment: false }; },
        token: function (stream, state) {
          if (state.inComment) {
            if (stream.sol()) state.inComment = false;
            else { stream.skipToEnd(); return 'comment'; }
          }
          if (stream.sol()) {
            if (stream.match(/^\s*\/\//)) { state.inComment = true; stream.skipToEnd(); return 'comment'; }
            if (stream.match(/^\s*>>\s+/)) { return 'keyword'; }
            if (stream.match(/^\s*-\s+/)) { return 'keyword'; }
          }
          if (stream.match(/^"[^"]*"/)) return 'string';
          if (stream.match(/^'[^']*'/)) return 'string';
          if (stream.match(/^(comoptions|labeloptions)(\.)/)) { stream.backUp(1); return 'property'; }
          if (stream.match(/^[\w]+\./)) return 'property';
          if (stream.match(/^[=-]/)) return 'operator';
          if (stream.match(/^(true|false)\b/)) return 'atom';
          if (stream.match(/^-?\d+(\.\d+)?/)) return 'number';
          if (stream.match(/^\s+/)) return null;
          stream.next();
          return null;
        }
      };
    });
  }

  global.DynDsl = {
    parseDsl: parseDsl,
    toDsl: toDsl,
    ALIASES: ALIASES,
    loadMeta: loadMeta,
    buildHintIndex: buildHintIndex,
    extractProps: extractProps,
    getHintIndex: getHintIndex,
    attachDslHints: attachDslHints,
    markDslIssues: markDslIssues,
    defineDslMode: defineDslMode
  };
})(window);

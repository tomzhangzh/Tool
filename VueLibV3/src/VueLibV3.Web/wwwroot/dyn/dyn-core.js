/* ============================================================
 * VueLibV3  dyn-core.js  动态渲染核心
 * ------------------------------------------------------------
 * 职责：
 *   1. DynRender 递归组件：JSON 配置 → VNode（设计时/运行时同一套代码）
 *   2. 组件解析：优先 DynCom 注册表，其次 Vue 全局注册（ElementPlus）
 *   3. modelname 双向绑定 / comlisteners 事件→动作 / slots / childrenctrls
 *   4. validators → el-form rules；labeloptions → el-form-item 包装
 *   5. 设计模式：不额外包装 div，选中/悬停/拖放事件直接落在组件根元素上
 *   6. normalize（补默认值）/ clean（空值剔除）工具
 * 所有静态变量集中在头部。
 * ============================================================ */
(function (global) {
  'use strict';

  var VueObj = global.Vue;
  var h = VueObj.h;
  var resolveComponent = VueObj.resolveComponent;

  /* ================= DynRender 组件 ================= */
  var DynRender = {
    name: 'DynRender',
    props: { cfg: { type: Object, required: true } },
    inject: {
      pageCtx: { default: null },
      formModel: { default: null },
      loopItem: { default: null },
      loopIndex: { default: null }
    },
    render: function () {
      var self = this;
      var cfg = this.cfg;
      if (!cfg || typeof cfg !== 'object') return h('div', { class: 'dyn-empty' }, '空配置');

      var component = cfg.component || 'DynText';
      var options = cfg.options || {};
      var comoptions = options.comoptions || {};
      var comlisteners = options.comlisteners || {};
      var labeloptions = options.labeloptions || {};
      var itemoptions = options.itemoptions || {};
      var validators = cfg.validators || [];
      var childrenctrls = cfg.childrenctrls || [];
      var slots = cfg.slots || {};

      /* ---- 解析组件 ---- */
      var comp = null;
      var DynCom = global.DynCom;
      if (DynCom && DynCom.get(component)) comp = DynCom.get(component);
      else comp = resolveComponent(component);
      if (!comp || typeof comp === 'string') {
        return h('div', { class: 'dyn-unknown' }, '未知组件: ' + component);
      }

      /* ---- props：comoptions（支持 __expr 动态表达式） ---- */
      var props = {};
      var isTextButton = (component === 'ElButton' || component === 'ElTag');
      Object.keys(comoptions).forEach(function (k) {
        // ElButton/ElTag 的 text 作为默认插槽内容渲染，不作为 prop 传入
        if (isTextButton && k === 'text') return;
        props[k] = evalValue(comoptions[k], self);
      });

      /* ---- 事件：comlisteners → DynActionHelper（Vue3 h() 事件键需 onXxx 前缀） ---- */
      var on = {};
      Object.keys(comlisteners || {}).forEach(function (evt) {
        on['on' + evt.charAt(0).toUpperCase() + evt.slice(1)] = function () {
          var args = Array.prototype.slice.call(arguments);
          return global.DynActionHelper.run(comlisteners[evt], { args: args }, pageCtxOf(self));
        };
      });

      /* ---- v-model 双向绑定 ---- */
      var model = this.formModel || (this.pageCtx && this.pageCtx.pageData);
      if (cfg.modelname && model) {
        props.modelValue = getPath(model, cfg.modelname);
        on['onUpdate:modelValue'] = function (v) { setPath(model, cfg.modelname, v); };
      }

      /* ---- 插槽 ---- */
      var slotVNodes = {};
      Object.keys(slots).forEach(function (name) {
        slotVNodes[name] = function () { return (slots[name] || []).map(function (c) { return h(DynRender, { cfg: c }); }); };
      });
      slotVNodes.default = function () { return childrenctrls.map(function (c) { return h(DynRender, { cfg: c }); }); };

      /* ---- 按钮/标签类 text 渲染到默认插槽 ---- */
      if ((component === 'ElButton' || component === 'ElTag') && comoptions.text && !childrenctrls.length) {
        slotVNodes.default = function () { return [comoptions.text]; };
      }

      /* ---- 外层属性：itemoptions.class/style + 设计模式 uid ---- */
      var outer = {};
      if (itemoptions.class) outer.class = itemoptions.class;
      if (itemoptions.style) outer.style = itemoptions.style;
      if (this.pageCtx && this.pageCtx.designMode && this.pageCtx.uidOf) {
        outer['data-dyn-uid'] = this.pageCtx.uidOf(cfg);
      }

      /* ---- 设计模式交互：选中/悬停/拖放（不包装 div，事件直接落在组件根元素） ---- */
      if (this.pageCtx && this.pageCtx.designMode) {
        outer.draggable = true; // 画布内组件可拖拽移动
        if (this.pageCtx.onDragStartCfg) {
          on.onDragstart = function (e) {
            e.stopPropagation();
            self.pageCtx.onDragStartCfg(cfg, e);
          };
        }
        if (this.pageCtx.onSelect) {
          on.onClick = function (e) {
            e.stopPropagation();
            // 再点一次当前已选中的组件 → 选中其父容器（否则永远选不中容器）
            var parent = (self.pageCtx.parentOf && self.pageCtx.selected === cfg) ? self.pageCtx.parentOf(cfg) : null;
            self.pageCtx.onSelect(parent || cfg, e);
          };
        }
        if (this.pageCtx.onHover) {
          on.onMouseover = function (e) { e.stopPropagation(); self.pageCtx.onHover(cfg, e); };
        }
        if (this.pageCtx.onDragOver) {
          on.onDragover = function (e) {
            e.preventDefault();
            e.stopPropagation();
            self.pageCtx.onDragOver(cfg, e);
          };
          on.onDrop = function (e) {
            e.preventDefault();
            e.stopPropagation();
            self.pageCtx.onDrop(cfg, e);
          };
          on.onDragleave = function (e) { e.stopPropagation(); self.pageCtx.onDragLeave(cfg, e); };
        }
      }

      /* ---- label 包装：labeloptions.show && (label 或 required) ---- */
      var showLabel = labeloptions && labeloptions.show !== false && (labeloptions.label || labeloptions.required);
      if (showLabel) {
        var formItemProps = {
          label: labeloptions.label,
          required: !!labeloptions.required,
          prop: cfg.modelname
        };
        if (labeloptions.labelWidth) formItemProps.labelWidth = labeloptions.labelWidth;
        if (validators.length) formItemProps.rules = global.DynValidator.toRules(validators);
        return h(ElFormItemComponent(), Object.assign({}, formItemProps, outer, on), {
          default: function () { return h(comp, Object.assign({}, props, on), slotVNodes); }
        });
      }
      return h(comp, Object.assign({}, props, on, outer), slotVNodes);
    }
  };

  /* 惰性获取 ElFormItem（避免加载时序问题） */
  var _elFormItem = null;
  function ElFormItemComponent() {
    if (!_elFormItem) {
      try { _elFormItem = resolveComponent('ElFormItem'); } catch (e) { _elFormItem = 'div'; }
    }
    return _elFormItem;
  }

  function pageCtxOf(instance) {
    return { pageCtx: instance.pageCtx, formModel: instance.formModel, loopItem: instance.loopItem, loopIndex: instance.loopIndex };
  }

  /* ================= 工具函数 ================= */

  /** 路径取值：getPath(obj, 'a.b.c') */
  function getPath(obj, path) {
    if (!obj || !path) return undefined;
    var segs = String(path).split('.');
    var cur = obj;
    for (var i = 0; i < segs.length; i++) {
      if (cur == null) return undefined;
      cur = cur[segs[i]];
    }
    return cur;
  }

  /** 路径赋值 */
  function setPath(obj, path, value) {
    if (!obj || !path) return;
    var segs = String(path).split('.');
    var cur = obj;
    for (var i = 0; i < segs.length - 1; i++) {
      if (cur[segs[i]] == null) cur[segs[i]] = {};
      cur = cur[segs[i]];
    }
    cur[segs[segs.length - 1]] = value;
  }

  /** 表达式求值：comoptions 中的 {__expr:'...'} 动态绑定 */
  function evalValue(v, instance) {
    if (v && typeof v === 'object' && '__expr' in v) {
      var expr = v.__expr;
      try {
        var ctx = pageCtxOf(instance);
        var fn = new Function('$ctx', '$form', '$item', '$index', 'return (' + expr + ');');
        return fn(ctx.pageCtx, ctx.formModel, ctx.loopItem, ctx.loopIndex);
      } catch (e) {
        console.warn('[DynCore] 表达式求值失败', expr, e);
        return null;
      }
    }
    return v;
  }

  /**
   * 补默认值：加载配置时统一处理（属性为 null/""/[]/{} 不入库，但加载时要有默认值）
   * 基础结构 + 组件元数据 PropsMeta 的 default
   */
  function normalize(cfg) {
    if (!cfg || typeof cfg !== 'object') return cfg;
    cfg.options = cfg.options || {};
    cfg.options.comoptions = cfg.options.comoptions || {};
    cfg.options.comlisteners = cfg.options.comlisteners || {};
    cfg.options.labeloptions = Object.assign({ label: '', required: false, show: true, labelWidth: '' }, cfg.options.labeloptions || {});
    cfg.options.itemoptions = Object.assign({ style: {}, class: '' }, cfg.options.itemoptions || {});
    cfg.validators = cfg.validators || [];
    cfg.childrenctrls = cfg.childrenctrls || [];
    cfg.slots = cfg.slots || {};
    cfg.extendinfo = cfg.extendinfo || {};

    // 组件元数据默认值
    var meta = (global.DynCom && global.DynCom.meta(cfg.component)) || null;
    if (meta && Array.isArray(meta.PropsMeta)) {
      meta.PropsMeta.forEach(function (pm) {
        if (pm && pm.name && cfg.options.comoptions[pm.name] === undefined && pm.default !== undefined) {
          cfg.options.comoptions[pm.name] = pm.default;
        }
      });
    }
    (cfg.childrenctrls || []).forEach(normalize);
    Object.keys(cfg.slots || {}).forEach(function (k) { (cfg.slots[k] || []).forEach(normalize); });
    return cfg;
  }

  /** 空值剔除：保存时 null/""/[]/{} 不入库 */
  function clean(cfg) {
    if (cfg === null || cfg === undefined) return undefined;
    if (Array.isArray(cfg)) {
      var arr = cfg.map(clean).filter(function (x) { return x !== undefined; });
      return arr.length ? arr : undefined;
    }
    if (typeof cfg === 'object') {
      if (cfg.__expr !== undefined) return cfg;
      var obj = {};
      var keys = Object.keys(cfg);
      if (!keys.length) return undefined;
      keys.forEach(function (k) {
        var v = clean(cfg[k]);
        if (isEmpty(v)) return;
        obj[k] = v;
      });
      return Object.keys(obj).length ? obj : undefined;
    }
    return cfg;
  }

  function isEmpty(v) {
    if (v === null || v === undefined) return true;
    if (typeof v === 'string') return v === '';
    if (Array.isArray(v)) return v.length === 0;
    if (typeof v === 'object') return Object.keys(v).length === 0;
    return false;
  }

  /** 便捷挂载：把配置渲染到指定元素 */
  function mount(cfg, el, overrides) {
    var app = VueObj.createApp({
      provide: function () {
        return {
          pageCtx: Object.assign({ pageData: VueObj.reactive({}) }, overrides || {})
        };
      },
      render: function () { return h(DynRender, { cfg: cfg }); }
    });
    if (global.DynCom) global.DynCom.setupApp(app);
    app.mount(el);
    return app;
  }

  global.DynCore = {
    DynRender: DynRender,
    getPath: getPath,
    setPath: setPath,
    evalValue: evalValue,
    normalize: normalize,
    clean: clean,
    mount: mount
  };
})(window);

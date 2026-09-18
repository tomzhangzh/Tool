/* dyn-render.js V4 独立递归渲染器，硬编码内核组件，不从后端加载 */
(function (global) {
'use strict';
const Vue = global.Vue;
if (!Vue) { console.error('[DynRender] Vue3 未加载，dyn-render.js 需要 Vue UMD'); return; }
const h = Vue.h;

/**
 * @description 对象路径取值，优先lodash _.get，无lodash使用简易降级实现
 * @param {any} obj 源对象
 * @param {string} path 点分隔路径，支持 arr[0]
 * @returns {any}
 * @demo getPath(model,"user.name")
 */
function getPath(obj, path) {
  if (!obj || !path) return undefined;
  if (global._ && typeof global._.get === 'function') {
    return global._.get(obj, path);
  }
  return String(path).split('.').reduce((o, k) => o == null ? undefined : o[k], obj);
}

/**
 * @description 对象路径赋值，优先lodash _.set，无lodash降级
 * @param {any} obj 目标对象
 * @param {string} path 点路径
 * @param {any} value 设置值
 * @demo setPath(model,"user.name","张三")
 */
function setPath(obj, path, value) {
  if (!obj || !path) return;
  if (global._ && typeof global._.set === 'function') {
    global._.set(obj, path, value);
    return;
  }
  const keys = String(path).split('.');
  const last = keys.pop();
  let cur = obj;
  for (let i = 0; i < keys.length; i++) {
    const k = keys[i];
    const m = k.match(/^(\w+)\[(\d+)\]$/);
    if (m) {
      const arrKey = m[1];
      const idx = Number(m[2]);
      if (!cur[arrKey]) cur[arrKey] = [];
      if (!cur[arrKey][idx]) cur[arrKey][idx] = {};
      cur = cur[arrKey][idx];
    } else if (/^\d+$/.test(k)) {
      const nIdx = Number(k);
      if (!Array.isArray(cur)) cur = [];
      if (!cur[nIdx]) cur[nIdx] = {};
      cur = cur[nIdx];
    } else {
      if (!cur[k] || typeof cur[k] !== 'object') cur[k] = {};
      cur = cur[k];
    }
  }
  cur[last] = value;
}

/**
 * @description 解析__expr表达式
 * @param {any} v cfg中的comoptions值
 * @param {object} instance 组件实例
 * @returns {any}
 */
function evalValue(v, instance) {
  if (v && typeof v === 'object' && '__expr' in v) {
    const expr = v.__expr;
    try {
      const ctx = {
        pageCtx: instance.pageCtx,
        formModel: instance.formModel,
        loopItem: instance.loopItem,
        loopIndex: instance.loopIndex
      };
      const fn = new Function('$ctx', '$form', '$item', '$index', 'return (' + expr + ');');
      return fn(ctx.pageCtx, ctx.formModel, ctx.loopItem, ctx.loopIndex);
    } catch (e) {
      console.warn('[DynRender]表达式求值失败', expr, e);
      return null;
    }
  }
  return v;
}

let _elFormItem = null;
/**
 * @description 安全获取ElFormItem组件，不存在返回div兜底
 */
function getElFormItem() {
  if (!_elFormItem) {
    try { _elFormItem = Vue.resolveComponent('ElFormItem'); } catch (e) { _elFormItem = 'div'; }
  }
  return _elFormItem;
}

/**
 * DynRender 递归渲染组件配置cfg，内核硬编码组件
 * @type {import("vue").Component}
 */
const DynRender = {
  name: "DynRender",
  props: {
    cfg: { type: Object, required: true }
  },
  inject: {
    pageCtx: { default: null },
    formModel: { default: null },
    loopItem: { default: null },
    loopIndex: { default: null }
  },
  render() {
    const self = this;
    const cfg = this.cfg;
    if (!cfg || typeof cfg !== 'object') return h('div', { class: 'dyn-empty' }, "空配置");
    const component = cfg.component || "DynText";
    const options = cfg.options || {};
    const comoptions = options.comoptions || {};
    const comlisteners = options.comlisteners || {};
    const labeloptions = options.labeloptions || {};
    const itemoptions = options.itemoptions || {};
    const validators = cfg.validators || [];
    const childrenctrls = cfg.childrenctrls || [];
    const slots = cfg.slots || {};

    let comp = null;
    const DynCom = global.DynCom;
    if (DynCom && DynCom.get(component)) {
      comp = DynCom.get(component);
    } else {
      comp = Vue.resolveComponent(component);
    }
    if (!comp || typeof comp === 'string') {
      return h('div', { class: 'dyn-unknown' }, "未知组件:" + component);
    }

    const props = {};
    const isTextBtn = (component === 'ElButton' || component === 'ElTag');
    Object.keys(comoptions).forEach(k => {
      if (isTextBtn && k === 'text') return;
      props[k] = evalValue(comoptions[k], self);
    });

    if (component.indexOf('El') !== 0) {
      props.jsonconfig = cfg;
      props.parentmodelinfo = this.formModel || {};
      props.nodePath = 'root';
    }

    const on = {};
    Object.keys(comlisteners || {}).forEach(evt => {
      const key = 'on' + evt.charAt(0).toUpperCase() + evt.slice(1);
      on[key] = (...args) => {
        if(global.dyn && global.dyn.resolveAction){
          const pageCtxOf = { pageCtx: self.pageCtx, formModel: self.formModel, loopItem: self.loopItem, loopIndex: self.loopIndex };
          global.dyn.runJsonActions({actions:[{action:comlisteners[evt]}]}, self.$el, pageCtxOf);
        }
      };
    });

    let model = this.formModel || (this.pageCtx && this.pageCtx.pageData);
    if (cfg.modelname && model) {
      props.modelValue = getPath(model, cfg.modelname);
      on['onUpdate:modelValue'] = (v) => setPath(model, cfg.modelname, v);
    }

    const slotVNodes = {};
    Object.keys(slots).forEach(name => {
      slotVNodes[name] = () => (slots[name] || []).map(c => h(DynRender, { cfg: c }));
    });
    slotVNodes.default = () => childrenctrls.map(c => h(DynRender, { cfg: c }));
    if ((component === 'ElButton' || component === 'ElTag') && comoptions.text && !childrenctrls.length) {
      slotVNodes.default = () => [comoptions.text];
    }

    const outer = {};
    if (itemoptions.class) outer.class = itemoptions.class;
    if (itemoptions.style) outer.style = itemoptions.style;
    if (this.pageCtx && this.pageCtx.designMode && this.pageCtx.uidOf) {
      outer['data-dyn-uid'] = this.pageCtx.uidOf(cfg);
    }

    if (this.pageCtx && this.pageCtx.designMode) {
      outer.draggable = true;
      if (this.pageCtx.onDragStartCfg) {
        on.onDragstart = (e) => { e.stopPropagation(); self.pageCtx.onDragStartCfg(cfg, e); };
      }
      if (this.pageCtx.onSelect) {
        on.onClickCapture = (e) => {
          const parent = (self.pageCtx.parentOf && self.pageCtx.selected === cfg) ? self.pageCtx.parentOf(cfg) : null;
          self.pageCtx.onSelect(parent || cfg, e);
        };
      }
      if (this.pageCtx.onHover) {
        on.onMouseover = (e) => { e.stopPropagation(); self.pageCtx.onHover(cfg, e); };
      }
      if (this.pageCtx.onDragOver) {
        on.onDragover = (e) => { e.preventDefault(); e.stopPropagation(); self.pageCtx.onDragOver(cfg, e); };
        on.onDrop = (e) => { e.preventDefault(); e.stopPropagation(); self.pageCtx.onDrop(cfg, e); };
        on.onDragleave = (e) => { e.stopPropagation(); self.pageCtx.onDragLeave(cfg, e); };
      }
    }

    const isFormItemItself = cfg.component === 'ElFormItem';
    const showLabel = !isFormItemItself && labeloptions && labeloptions.show !== false && (labeloptions.label || labeloptions.required);

    if (showLabel) {
      const fiProps = {
        label: labeloptions.label,
        required: !!labeloptions.required,
        prop: cfg.modelname
      };
      if (labeloptions.labelWidth) fiProps.labelWidth = labeloptions.labelWidth;
      if (validators.length && global.DynValidator) fiProps.rules = global.DynValidator.toRules(validators);
      return h(getElFormItem(), Object.assign({}, fiProps, outer, on), {
        default: () => h(comp, Object.assign({}, props, on), slotVNodes)
      });
    }
    if (isFormItemItself) {
      if (labeloptions && labeloptions.label) props.label = labeloptions.label;
      if (labeloptions && labeloptions.required) props.required = true;
    }

    const designOn = {};
    if (this.pageCtx && this.pageCtx.designMode) {
      ['onDragstart', 'onClickCapture', 'onMouseover', 'onDragover', 'onDrop', 'onDragleave'].forEach(k => {
        if (on[k]) { designOn[k] = on[k]; delete on[k]; }
      });
    }
    const compAttrs = {};
    const compClass = [];
    if (outer.class) compClass.push(outer.class);
    if (compClass.length) compAttrs.class = compClass.join(' ');
    if (outer.style) compAttrs.style = outer.style;

    const wrapProps = { style: { display: 'contents' } };
    if (outer['data-dyn-uid']) wrapProps['data-dyn-uid'] = outer['data-dyn-uid'];
    if (outer.draggable) wrapProps.draggable = outer.draggable;
    return h('span', Object.assign(wrapProps, designOn), [
      h(comp, Object.assign({}, props, on, compAttrs), slotVNodes)
    ]);
  }
};

global.DynRender = DynRender;
})(window);

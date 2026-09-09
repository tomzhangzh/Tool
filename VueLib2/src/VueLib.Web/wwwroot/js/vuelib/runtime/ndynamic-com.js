/**
 * VueLib runtime/ndynamic-com：唯一渲染内核（NDynamicCom / LCNode）
 *
 * 设计原则：
 *  - 运行时与设计器共用此内核，不写两套渲染逻辑。
 *  - design=true 时：每个节点外层包 <div class="lc-node">（选中框/工具栏/拖拽），
 *    内部渲染逻辑与运行时完全一致。
 *  - 组件递归由容器模板内的 <lc-node v-for="child in safeChildren"> 驱动。
 *
 * 设计模式拖拽（Sortable.js）：
 *  - 容器内容根元素挂 Sortable（group lc-designer-group），支持画布内/跨容器排序；
 *  - 左侧组件库走原生 HTML5 DnD（dataTransfer MIME: application/x-lc-comp），
 *    容器 dragover/drop 显示占位线并新增组件。
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;

    var PALETTE_MIME = 'application/x-lc-comp';

    var NDynamicCom = {
        name: 'NDynamicCom',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, required: false, default: null },
            design: { type: Boolean, default: false },
            designApi: { type: Object, default: null },
            nodePath: { type: String, default: 'root' },
            depth: { type: Number, default: 0 }
        },
        setup: function (props) {
            var jc = Vue.computed(function () { return props.jsonconfig || {}; });
            var design = !!props.design;
            var designApi = props.designApi || null;
            var depth = props.depth || 0;

            var compName = Vue.computed(function () { return window.VueLib.services.component.resolveComponentName(jc.value.component); });
            var tooDeep = depth > C.designDepthLimit;

            var isSelected = Vue.computed(function () {
                return designApi && typeof designApi.isSelected === 'function' &&
                    designApi.isSelected(props.nodePath);
            });
            var isLocked = Vue.computed(function () {
                return designApi && typeof designApi.isLocked === 'function' &&
                    designApi.isLocked(props.nodePath);
            });
            // 节点 itemoptions 样式（设计器包装层透传：grid-column 等子项定位必须作用在 grid 直接子项上；
            // 但 display/grid-template/flex 等"容器自身布局"属性不透明传，避免与组件自身 div 冲突）
            var WRAP_STYLE_FILTER = ['display', 'grid-template-columns', 'grid-template-rows', 'grid-template-areas',
                'gap', 'row-gap', 'column-gap', 'grid-gap',
                'flex-direction', 'flex-wrap', 'justify-content', 'align-items', 'align-content',
                'grid-auto-flow', 'grid-auto-columns', 'grid-auto-rows'];
            var wrapStyle = Vue.computed(function () {
                var n = jc.value && jc.value.options && jc.value.options.itemoptions;
                var s = (n && n.style) || null;
                if (!s || typeof s !== 'object') return null;
                var out = {};
                for (var k in s) {
                    if (WRAP_STYLE_FILTER.indexOf(k) >= 0) continue;
                    out[k] = s[k];
                }
                return Object.keys(out).length ? out : null;
            });
            var wrapClass = Vue.computed(function () {
                var n = jc.value && jc.value.options && jc.value.options.itemoptions;
                return (n && n.class) || '';
            });

            // 设计器注入（提供给 Razor 组件 setup 的 inject('design_ShareData')）
            Vue.provide('design_ShareData', design ? { designApi: designApi, nodePath: props.nodePath } : null);
            Vue.provide('lcDesigner', designApi);
            Vue.provide('parent_jsonconfig', jc.value);
            Vue.provide('parentModelPrefix', Vue.computed(function () { return ''; }));  // 根节点前缀为空

            function onNodeClick(e) {
                if (designApi && typeof designApi.select === 'function') {
                    designApi.select(props.nodePath, jc.value, e);
                }
            }

            var nodeClass = Vue.computed(function () {
                var cls = ['lc-node'];
                if (isSelected.value) cls.push('lc-selected');
                if (isLocked.value) cls.push('lc-locked');
                return cls;
            });

            // 容器（设计模式）显示拖放区，接收左侧组件库拖入 / 画布内移动
            var isContainer = Vue.computed(function () { return window.VueLib.designer ? window.VueLib.designer.isContainer(jc.value.component) : false; });
            // 仅空容器显示"＋ 拖入组件"提示
            var isEmpty = Vue.computed(function () {
                return !((jc.value.childrenctrls || []).filter(function (c) { return c && c.component; }).length);
            });

            /* ============ 设计模式容器：Sortable 排序 + palette 原生拖入 ============ */
            var instance = Vue.getCurrentInstance();  // setup 同步期间捕获（onMounted 内调用会返回 null）
            var contentRoot = null;   // 容器内容根元素（component 渲染根）
            var sortableInst = null;
            var boundHost = null;     // 已绑定 Sortable 的 DOM（updated 重建判断）
            var lastAddAt = 0;        // onAdd 防重时间戳

            // 取容器内容根：wrap > .lc-node 下第一个非装饰元素（component 渲染根）
            function findContentRoot(wrapEl) {
                if (!wrapEl) return null;
                var nodeEl = wrapEl.querySelector('.lc-node');
                if (!nodeEl) return null;
                for (var i = 0; i < nodeEl.children.length; i++) {
                    var c = nodeEl.children[i];
                    if (c.classList && (
                        c.classList.contains('lc-node-toolbar') ||
                        c.classList.contains('lc-lock-mask') ||
                        c.classList.contains('lc-depth-error'))) continue;
                    return c;
                }
                return null;
            }

            function isPaletteDrag(e) {
                return !!(e.dataTransfer && e.dataTransfer.types &&
                    Array.prototype.indexOf.call(e.dataTransfer.types, PALETTE_MIME) >= 0);
            }

            function computeInsertIndex(el, e) {
                var kids = Array.prototype.filter.call(el.children, function (c) {
                    return c.classList && c.classList.contains('lc-node-wrap');
                });
                var idx = kids.length;
                var horizontal = false;
                if (kids.length > 1) {
                    var r0 = kids[0].getBoundingClientRect();
                    var rn = kids[kids.length - 1].getBoundingClientRect();
                    horizontal = Math.abs(r0.top - rn.top) < Math.min(r0.height, 12);
                }
                for (var i = 0; i < kids.length; i++) {
                    var r = kids[i].getBoundingClientRect();
                    var hit = horizontal
                        ? (e.clientX < r.left + r.width / 2)
                        : (e.clientY < r.top + r.height / 2);
                    if (hit) { idx = i; break; }
                }
                return idx;
            }

            function showDropPlaceholder(el, e) {
                var ph = document.querySelector('.lc-drop-placeholder');
                if (!ph) {
                    ph = document.createElement('div');
                    ph.className = 'lc-drop-placeholder';
                    document.body.appendChild(ph);
                }
                var idx = computeInsertIndex(el, e);
                var kids = Array.prototype.filter.call(el.children, function (c) {
                    return c.classList && c.classList.contains('lc-node-wrap');
                });
                var rect;
                if (kids.length > 0 && idx >= kids.length) {
                    var r1 = kids[kids.length - 1].getBoundingClientRect();
                    rect = { left: r1.left, top: r1.bottom - 3, width: r1.width };
                } else if (kids.length > 0) {
                    var r2 = kids[idx].getBoundingClientRect();
                    rect = { left: r2.left, top: r2.top - 3, width: r2.width };
                } else {
                    var r3 = el.getBoundingClientRect();
                    rect = { left: r3.left + 8, top: r3.top + r3.height / 2 - 3, width: r3.width - 16 };
                }
                ph.style.cssText = 'position:fixed;left:' + rect.left + 'px;top:' + rect.top + 'px;width:' + rect.width + 'px;height:6px;background:#2563eb;border-radius:3px;z-index:9998;pointer-events:none;box-shadow:0 0 8px rgba(37,99,235,.7);';
                el.classList.add('lc-drop-target');
            }

            function hideDropPlaceholder(el) {
                var ph = document.querySelector('.lc-drop-placeholder');
                if (ph) ph.remove();
                if (el) el.classList.remove('lc-drop-target');
            }

            function onPaletteDragOver(e) {
                if (!isPaletteDrag(e)) return;
                e.preventDefault();
                if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
                if (contentRoot) showDropPlaceholder(contentRoot, e);
            }

            function onPaletteDragLeave(e) {
                if (!isPaletteDrag(e)) return;
                if (!e.relatedTarget || (contentRoot && !contentRoot.contains(e.relatedTarget))) {
                    hideDropPlaceholder(contentRoot);
                }
            }

            function onPaletteDrop(e) {
                if (!isPaletteDrag(e)) return;
                e.preventDefault();
                e.stopPropagation();  // 只让最内层容器处理
                var name = e.dataTransfer.getData(PALETTE_MIME);
                hideDropPlaceholder(contentRoot);
                if (designApi && typeof designApi.addComponent === 'function' && name) {
                    var idx = contentRoot ? computeInsertIndex(contentRoot, e) : undefined;
                    designApi.addComponent(props.nodePath, name, idx);
                }
            }

            // 兼容 Fragment：组件模板多根时 $el 是注释锚点，需从 subTree 找真实元素
            function getRootEl() {
                if (!instance) return null;
                var el = instance.proxy && instance.proxy.$el;
                if (el && el.nodeType === 1) return el;
                var sub = instance.subTree;
                if (sub) {
                    if (sub.el && sub.el.nodeType === 1) return sub.el;
                    if (Array.isArray(sub.children)) {
                        for (var i = 0; i < sub.children.length; i++) {
                            var c = sub.children[i];
                            if (c && c.el && c.el.nodeType === 1) return c.el;
                        }
                    }
                }
                return null;
            }

            function bindSortable() {
                if (!design || !isContainer.value || !window.Sortable) return;
                if (!instance || !instance.proxy) return;
                var wrapEl = getRootEl();
                if (!wrapEl) return;
                var root = findContentRoot(wrapEl);
                if (!root) return;
                contentRoot = root;
                root.setAttribute('data-lc-path', props.nodePath);
                // 已有绑定则先销毁（updated 后 DOM 重建）
                if (boundHost === root && sortableInst) return;
                if (sortableInst) { try { sortableInst.destroy(); } catch (e) { /* 忽略 */ } sortableInst = null; }
                if (boundHost) { try { boundHost.__lcSortable = null; } catch (e) { /* 忽略 */ } }
                sortableInst = window.Sortable.create(root, {
                    group: { name: 'lc-designer-group', pull: true, put: true },
                    animation: 150,
                    ghostClass: 'lc-ghost',
                    dragClass: 'lc-drag',
                    chosenClass: 'lc-chosen',
                    draggable: '> .lc-node-wrap',
                    onStart: function (evt) {
                        // 锁定节点禁止拖动
                        var nodeEl = evt.item && evt.item.querySelector('.lc-node');
                        if (nodeEl && nodeEl.classList.contains('lc-locked')) {
                            if (evt.preventDefault) evt.preventDefault();
                            return false;
                        }
                    },
                    onEnd: function (evt) {
                        if (designApi && typeof designApi.sortEnd === 'function') {
                            designApi.sortEnd(props.nodePath, evt);
                        }
                    }
                });
                root.__lcSortable = sortableInst;
                boundHost = root;
                // palette 原生 DnD 监听（Sortable 不接收外部 group 元素）
                root.removeEventListener('dragover', onPaletteDragOver);
                root.removeEventListener('dragleave', onPaletteDragLeave);
                root.removeEventListener('drop', onPaletteDrop);
                root.addEventListener('dragover', onPaletteDragOver);
                root.addEventListener('dragleave', onPaletteDragLeave);
                root.addEventListener('drop', onPaletteDrop);
            }

            function unbindSortable() {
                if (sortableInst) { try { sortableInst.destroy(); } catch (e) { /* 忽略 */ } sortableInst = null; }
                if (boundHost) {
                    boundHost.removeEventListener('dragover', onPaletteDragOver);
                    boundHost.removeEventListener('dragleave', onPaletteDragLeave);
                    boundHost.removeEventListener('drop', onPaletteDrop);
                    boundHost = null;
                }
            }

            Vue.onMounted(function () { if (design) bindSortable(); });
            // 排序后 nodePath 变化时同步 data-lc-path（Sortable 回调依赖此属性读容器路径）
            Vue.watch(function () { return props.nodePath; }, function (newPath) {
                if (contentRoot) contentRoot.setAttribute('data-lc-path', newPath);
            });
            Vue.onUpdated(function () { if (design && isContainer.value) bindSortable(); });
            Vue.onUnmounted(function () { unbindSortable(); });
            // 兜底：首帧后若仍未绑定则重试（onMounted 时序保险）
            setTimeout(function () { if (design && isContainer.value && !sortableInst) bindSortable(); }, 400);
            return {
                props: props, jc: jc, compName: compName, design: design, designApi: designApi,
                nodePath: props.nodePath, depth: depth, tooDeep: tooDeep,
                parentmodelinfo: props.parentmodelinfo,
                isSelected: isSelected, isLocked: isLocked, nodeClass: nodeClass,
                wrapStyle: wrapStyle, wrapClass: wrapClass,
                onNodeClick: onNodeClick,
                isContainer: isContainer, isEmpty: isEmpty
            };
        },
        template: '' +
            '<div v-if="design" class="lc-node-wrap" :data-node-path="props.nodePath" :style="wrapStyle" :class="wrapClass">' +
            '  <div :class="nodeClass" @click.stop="onNodeClick">' +
            '    <div v-if="tooDeep" class="lc-depth-error">组件树深度超出限制（> {{ props.depth }}），疑似循环引用</div>' +
            '    <component v-else :is="compName" :jsonconfig="jc" :parentmodelinfo="props.parentmodelinfo" :nodePath="props.nodePath" :design="design" :designApi="designApi" :depth="props.depth"></component>' +
            '    <div v-if="isSelected && designApi" class="lc-node-toolbar" @click.stop @mousedown.stop>' +
            '      <span class="lc-node-name">{{ jc.component }}</span>' +
            '      <button v-if="designApi.moveUp" @click.stop="designApi.moveUp(props.nodePath)">↑</button>' +
            '      <button v-if="designApi.moveDown" @click.stop="designApi.moveDown(props.nodePath)">↓</button>' +
            '      <button v-if="designApi.copy" @click.stop="designApi.copy(props.nodePath)">复制</button>' +
            '      <button v-if="designApi.toggleLock" @click.stop="designApi.toggleLock(props.nodePath)">{{ isLocked ? "解锁" : "锁定" }}</button>' +
            '      <button v-if="designApi.remove" class="lc-btn-danger" @click.stop="designApi.remove(props.nodePath)">删除</button>' +
            '    </div>' +
            '    <div v-if="isLocked" class="lc-lock-mask"><span>已锁定</span></div>' +
            '  </div>' +
            '  <div v-if="design && isContainer && isEmpty" class="lc-dropzone">' +
            '    <span class="lc-dropzone-hint">＋ 拖入组件</span>' +
            '  </div>' +
            '</div>' +
            '<component v-else :is="compName" :jsonconfig="jc" :parentmodelinfo="props.parentmodelinfo" :nodePath="props.nodePath" :design="false" :designApi="null" :depth="props.depth"></component>'
    };

    window.VueLib = window.VueLib || {};
    window.VueLib.runtime = window.VueLib.runtime || {};
    window.VueLib.runtime.NDynamicCom = NDynamicCom;
    window.VueLib.runtime.LCNodeName = 'LCNode';
})();

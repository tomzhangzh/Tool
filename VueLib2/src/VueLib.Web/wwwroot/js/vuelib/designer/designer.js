/**
 * VueLib designer/designer：可视化设计器（单一 Vue App）
 *  - 左侧：组件库 Palette（分类，拖拽到画布）+ 组件树
 *  - 中间：画布 Canvas（复用运行时 NDynamicCom 内核，design 模式装饰）
 *  - 右侧：属性面板（通用 comoptions / 下拉多源 / flexbox 生成器 / 校验器 / 动作链 / 版本锁定 / 组合组件）
 *  - 顶部：保存 / 新建 / 预览 / 刷新组件 / JSON / Model
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;
    var NDynamicCom = window.VueLib.runtime.NDynamicCom;

    /* ---------- 容器组件集合（允许拖入子组件） ---------- */
    var CONTAINERS = ['DivContainer', 'Row', 'Col', 'Card', 'Tabs', 'Slot', 'Fragment', 'ForEach'];
    function isContainer(name) { return CONTAINERS.indexOf(name) >= 0; }

    /* ---------- 路径工具 ---------- */
    function parentOf(path) {
        if (!path || path === 'root') return null;
        var i = path.lastIndexOf('.');
        if (i < 0) return 'root';
        return path.substring(0, i);
    }
    function lastKey(path) {
        if (!path || path === 'root') return 'root';
        var i = path.lastIndexOf('.');
        return i < 0 ? path : path.substring(i + 1);
    }
    function childPath(path, key) {
        return path === 'root' ? key : path + '.' + key;
    }
    var ARR_RE = /^(childrenctrls|slots\.[\w-]+\.childrenctrls)\[(\d+)\]$/;

    var _nodeIdSeq = 0;
    function blankNode(component) {
        _nodeIdSeq++;
        // 按组件类型给默认友好名和默认选项
        var friendly = {
            ElementInput: '输入框', ElementTextarea: '多行文本', ElementSelect: '下拉选择',
            ElementCheckboxGroup: '复选组', ElementRadioGroup: '单选组', ElementSwitch: '开关',
            ElementDatePicker: '日期选择', ElementButton: '按钮', ElementDivider: '分割线',
            DivContainer: '容器', Row: '行', Col: '列', Card: '卡片', Tabs: '标签页',
            Slot: '插槽', Fragment: '透明片段', ForEach: '循环', LabelWrapper: '标签包装'
        };
        var label = friendly[component] || component;
        var defaultComoptions = {};
        if (component === 'ElementInput') defaultComoptions = { placeholder: '请输入' + label };
        else if (component === 'ElementTextarea') defaultComoptions = { placeholder: '请输入' + label, rows: 3 };
        else if (component === 'ElementSelect') defaultComoptions = { placeholder: '请选择' };
        else if (component === 'ElementButton') defaultComoptions = { text: '按钮', type: 'primary' };
        else if (component === 'ElementDatePicker') defaultComoptions = { placeholder: '选择日期' };
        return {
            _id: 'n_' + Date.now().toString(36) + '_' + _nodeIdSeq,
            component: component,
            modelname: null,
            options: {
                comoptions: defaultComoptions,
                comlisteners: {},
                labeloptions: { label: label, required: false, show: true },
                itemoptions: { style: {}, class: '' },
                wrapperoptions: {}
            },
            validators: [],
            childrenctrls: [],
            slots: {},
            extendinfo: {}
        };
    }

    /* ================================================================
     * 设计器主组件
     * ================================================================ */
    var Designer = {
        name: 'LCDesigner',
        props: { page: { type: String, default: null } },
        components: { LCNode: NDynamicCom },
        data: function () {
            return {
                loading: true,
                metaList: [],
                metaMap: {},
                pageCode: null,
                pageName: '',
                configObj: blankNode('DivContainer'),
                modelObj: {},
                selectedPath: null,
                treeData: [],
                categoryFilter: 'All',
                showJson: false,
                jsonText: '',
                showModel: false,
                modelText: '',
                showCompositeDlg: false,
                compositeCfg: null,
                saving: false,
                paletteCollapsed: false,   // 左侧面板折叠
                collapsedCats: {},          // 分类分组折叠 {category: true}
                paletteGhost: null,         // 拖拽 ghost 元素
                dragLeaveTimer: null
            };
        },
        computed: {
            selectedNode: function () {
                if (!this.selectedPath) return null;
                if (this.selectedPath === 'root') return this.configObj;
                return Utils.getByPath(this.configObj, this.selectedPath) || null;
            },
            categories: function () {
                var set = {};
                this.metaList.forEach(function (m) { set[m.category] = true; });
                return ['All'].concat(Object.keys(set).sort());
            },
            paletteGroups: function () {
                var self = this;
                var list = this.metaList.filter(function (m) {
                    return self.categoryFilter === 'All' || m.category === self.categoryFilter;
                });
                var groups = {};
                list.forEach(function (m) { (groups[m.category] = groups[m.category] || []).push(m); });
                return Object.keys(groups).sort().map(function (k) { return { category: k, items: groups[k] }; });
            },
            flexboxCfg: {
                get: function () {
                    var n = this.selectedNode;
                    if (!n) return null;
                    n.options = n.options || {};
                    n.options.comoptions = n.options.comoptions || {};
                    if (!n.options.comoptions.flexbox) {
                        n.options.comoptions.flexbox = { enabled: true, direction: 'row', justify: 'start', align: 'stretch', wrap: 'wrap', gap: 4, className: '' };
                    }
                    return n.options.comoptions.flexbox;
                }
            },
            formLayoutCfg: {
                get: function () {
                    var n = this.selectedNode;
                    if (!n) return null;
                    n.options = n.options || {};
                    n.options.comoptions = n.options.comoptions || {};
                    if (!n.options.comoptions.formLayout) n.options.comoptions.formLayout = {};
                    return n.options.comoptions.formLayout;
                }
            },
            flexPreview: function () {
                var f = this.flexboxCfg;
                if (!f || !f.enabled) return '';
                var cls = ['flex'];
                cls.push(f.direction === 'col' ? 'flex-col' : 'flex-row');
                cls.push('justify-' + (f.justify || 'start'));
                cls.push('items-' + (f.align || 'stretch'));
                cls.push(f.wrap === 'nowrap' ? 'flex-nowrap' : 'flex-wrap');
                if (f.gap) cls.push('gap-' + f.gap);
                return cls.join(' ');
            },
            itemStyleText: {
                get: function () {
                    var n = this.selectedNode;
                    if (!n) return '';
                    var s = n.options.itemoptions.style;
                    return (s && typeof s === 'object') ? JSON.stringify(s, null, 2) : (s || '');
                },
                set: function (text) {
                    var n = this.selectedNode;
                    if (!n) return;
                    try { n.options.itemoptions.style = text ? JSON.parse(text) : {}; }
                    catch (e) { n.options.itemoptions.style = text || {}; }
                }
            },
            chainText: {
                get: function () {
                    var n = this.selectedNode;
                    if (!n) return '';
                    var chain = n.extendinfo && n.extendinfo.actions && n.extendinfo.actions.chain;
                    return chain ? JSON.stringify(chain, null, 2) : '';
                },
                set: function (text) {
                    var n = this.selectedNode;
                    if (!n) return;
                    try {
                        var parsed = JSON.parse(text || '[]');
                        n.extendinfo = n.extendinfo || {};
                        n.extendinfo.actions = n.extendinfo.actions || {};
                        n.extendinfo.actions.chain = Array.isArray(parsed) ? parsed : [];
                    } catch (e) {
                        n.extendinfo = n.extendinfo || {};
                        n.extendinfo.actions = n.extendinfo.actions || {};
                    }
                }
            },
            pageVersionsTable: function () {
                var map = {};
                this.metaList.forEach(function (m) { map[m.name] = m; });
                var locked = (this.configObj.extendinfo && this.configObj.extendinfo.componentVersions) || {};
                var used = {};
                var self = this;
                (function walk(node) {
                    if (!node || !node.component) return;
                    used[node.component] = true;
                    (node.childrenctrls || []).forEach(walk);
                    Object.keys(node.slots || {}).forEach(function (k) {
                        ((node.slots[k] || {}).childrenctrls || []).forEach(walk);
                    });
                })(this.configObj);
                var rows = [];
                Object.keys(used).forEach(function (name) {
                    var meta = map[name];
                    if (!meta) return;
                    rows.push({
                        name: name,
                        versions: meta.versions || [],
                        lockedVersion: locked[name] || 0
                    });
                });
                return rows;
            }
        },
        watch: {
            showJson: function (v) { if (v) this.jsonText = JSON.stringify(this.configObj, null, 2); },
            showModel: function (v) { if (v) this.modelText = JSON.stringify(this.modelObj, null, 2); },
            selectedPath: function () { this.$nextTick(function () { this.refreshTree(); }); }
        },
        methods: {
            /* ============ 设计API（NDynamicCom 调用） ============ */
            designApi: function () {
                var self = this;
                return {
                    select: function (p, j, e) { return self.onSelect(p, j, e); },
                    isSelected: function (p) { return self.isSelected(p); },
                    isLocked: function (p) { return self.isLocked(p); },
                    remove: function (p) { return self.onRemove(p); },
                    copy: function (p) { return self.onCopy(p); },
                    moveUp: function (p) { return self.onMoveUp(p); },
                    moveDown: function (p) { return self.onMoveDown(p); },
                    toggleLock: function (p) { return self.onToggleLock(p); },
                    dragStartNode: function (p, e) { return self.onDragStartNode(p, e); },
                    dropInto: function (p, j, e) { return self.onDropInto(p, j, e); },
                    dragOverNode: function (p, e) { return self.onDragOverNode(p, e); },
                    dragLeaveNode: function (e) { return self.onDragLeaveNode(e); },
                    clearPlaceholder: function () { return self.clearPlaceholder(); },
                    sortEnd: function (p, e) { return self.onSortEnd(p, e); },
                    addComponent: function (p, n, i) { return self.onAddComponent(p, n, i); }
                };
            },
            nodeAt: function (path) {
                if (!path || path === 'root') return this.configObj;
                return Utils.getByPath(this.configObj, path);
            },
            parentNodeOf: function (path) {
                var p = parentOf(path);
                return p === null ? null : this.nodeAt(p);
            },
            ensureDefaults: function (node) {
                if (!node) return node;
                // 补唯一 id（v-for key 用，排序时节点身份稳定，避免同类型组件复用导致 props 错位）
                if (!node._id) node._id = 'n_' + Date.now().toString(36) + '_' + (++_nodeIdSeq);
                node.options = node.options || {};
                node.options.comoptions = node.options.comoptions || {};
                node.options.comlisteners = node.options.comlisteners || {};
                node.options.labeloptions = Object.assign({ label: '', required: false, show: true }, node.options.labeloptions);
                node.options.itemoptions = Object.assign({ style: {}, class: '' }, node.options.itemoptions);
                node.options.wrapperoptions = node.options.wrapperoptions || {};
                node.validators = node.validators || [];
                node.childrenctrls = node.childrenctrls || [];
                node.slots = node.slots || {};
                node.extendinfo = node.extendinfo || {};
                // 递归补子节点 id
                var self = this;
                (node.childrenctrls || []).forEach(function (c) { self.ensureDefaults(c); });
                Object.keys(node.slots || {}).forEach(function (k) {
                    (node.slots[k].childrenctrls || []).forEach(function (c) { self.ensureDefaults(c); });
                });
                return node;
            },
            onSelect: function (path) {
                this.ensureDefaults(this.nodeAt(path));
                this.selectedPath = path;
            },
            isSelected: function (path) { return this.selectedPath === path; },
            isLocked: function (path) {
                var n = this.nodeAt(path);
                return !!(n && n.extendinfo && n.extendinfo.locked);
            },
            onToggleLock: function (path) {
                var n = this.ensureDefaults(this.nodeAt(path));
                n.extendinfo.locked = !n.extendinfo.locked;
            },
            onRemove: function (path) {
                if (path === 'root') { this.$message.warning('不能删除根节点'); return; }
                var parent = this.parentNodeOf(path);
                var m = ARR_RE.exec(lastKey(path));
                if (m && parent) {
                    var arr = m[1].indexOf('slots') === 0 ? Utils.getByPath(parent, m[1]) : parent.childrenctrls;
                    if (arr) arr.splice(parseInt(m[2]), 1);
                }
                if (this.selectedPath === path) this.selectedPath = null;
            },
            onCopy: function (path) {
                if (path === 'root') return;
                var parent = this.parentNodeOf(path);
                var m = ARR_RE.exec(lastKey(path));
                if (m && parent) {
                    var arr = m[1].indexOf('slots') === 0 ? Utils.getByPath(parent, m[1]) : parent.childrenctrls;
                    if (arr) arr.splice(parseInt(m[2]) + 1, 0, JSON.parse(JSON.stringify(arr[parseInt(m[2])])));
                }
            },
            move: function (path, dir) {
                if (path === 'root') return;
                var parent = this.parentNodeOf(path);
                var m = ARR_RE.exec(lastKey(path));
                if (!m || !parent) return;
                var arr = m[1].indexOf('slots') === 0 ? Utils.getByPath(parent, m[1]) : parent.childrenctrls;
                var idx = parseInt(m[2]);
                var to = idx + dir;
                if (arr && to >= 0 && to < arr.length) {
                    var t = arr[idx]; arr[idx] = arr[to]; arr[to] = t;
                }
                this.refreshTree();
            },
            onMoveUp: function (path) { this.move(path, -1); },
            onMoveDown: function (path) { this.move(path, 1); },

            /* ---------- 拖拽（ghost 跟随 + 插入占位线） ---------- */
            createGhost: function (meta) {
                this.removeGhost();
                var ghost = document.createElement('div');
                ghost.className = 'lc-palette-ghost';
                var icon = this.lcIcon(meta ? meta.name : '');
                var label = (meta && (meta.displayName || meta.name)) || '组件';
                ghost.innerHTML = '<span class="gh-icon">' + icon + '</span><span class="gh-label">' + label + '</span><span class="gh-add">＋</span>';
                ghost.style.cssText = 'position:fixed;top:-2000px;left:-2000px;pointer-events:none;z-index:9999;';
                document.body.appendChild(ghost);
                this.paletteGhost = ghost;
                return ghost;
            },
            removeGhost: function () {
                if (this.paletteGhost) { this.paletteGhost.remove(); this.paletteGhost = null; }
                document.removeEventListener('dragover', this._ghostMove);
            },
            _ghostMove: function (e) {
                var d = document.querySelector('.lc-palette-ghost');
                if (d) d.style.cssText = 'position:fixed;left:' + (e.clientX + 12) + 'px;top:' + (e.clientY + 8) + 'px;pointer-events:none;z-index:9999;';
            },
            onPaletteDrag: function (e, name) {
                var meta = this.metaMap[name] || null;
                e.dataTransfer.setData('application/x-lc-comp', name);
                e.dataTransfer.setData('text/plain', name);
                e.dataTransfer.effectAllowed = 'copy';
                this.createGhost(meta);
                var self = this;
                this._ghostMove = function (ev) {
                    var d = self.paletteGhost;
                    if (d) d.style.cssText = 'position:fixed;left:' + (ev.clientX + 12) + 'px;top:' + (ev.clientY + 8) + 'px;pointer-events:none;z-index:9999;';
                };
                document.addEventListener('dragover', this._ghostMove);
            },
            onPaletteDragEnd: function () {
                this.removeGhost();
                this.clearPlaceholder();
            },
            onPaletteClick: function (name) {
                var target = this.selectedNode && isContainer(this.selectedNode.component)
                    ? this.selectedNode : this.configObj;
                if (!isContainer(target.component)) {
                    this.$message.warning('请先选中一个容器组件（如 容器/卡片/行）再点击添加');
                    return;
                }
                target.childrenctrls = target.childrenctrls || [];
                target.childrenctrls.push(blankNode(name));
                this.refreshTree();
            },
            lcIcon: function (name) {
                var map = {
                    ElementButton: '🔘', ElementInput: '📝', ElementTextArea: '📄', ElementInputNumber: '🔢',
                    ElementSelect: '🔽', ElementCheckboxGroup: '☑️', ElementRadioGroup: '🔘', ElementDatePicker: '📅',
                    ElementSwitch: '🔀', LabelWrapper: '🏷️', ElementDivider: '➖', DivContainer: '📦', Row: '↔️',
                    Col: '↕️', Card: '🃏', Tabs: '📑', Slot: '🧩', Fragment: '🪶', ForEach: '🔁', Window: '🪟',
                    Grid3: '📊', Button: '🔘', CustomerCard: '🧱'
                };
                return map[name] || '📦';
            },
            onDragStartNode: function (path, e) {
                if (this.isLocked(path)) { e.preventDefault(); return; }
                e.dataTransfer.setData('lc-path', path);
                e.dataTransfer.effectAllowed = 'move';
            },
            /* 容器 dragover：显示插入占位线 + 容器高亮 */
            onDragOverNode: function (path, e) {
                var jc = path === 'root' ? this.configObj : Utils.getByPath(this.configObj, path);
                if (!jc) return;
                if (this.isLocked(path)) return;
                if (!isContainer(jc.component)) return;
                var containerEl = e.currentTarget;
                if (!containerEl) return;
                // 容器内容根：wrap > .lc-node > 组件根元素（如容器 div），其直接子节点是子 lc-node-wrap
                var nodeEl = containerEl.querySelector(':scope > .lc-node');
                var contentRoot = nodeEl ? nodeEl.firstElementChild : null;
                var nodes = contentRoot
                    ? Array.from(contentRoot.querySelectorAll(':scope > .lc-node-wrap'))
                    : [];
                // 方向：子节点横向排列（同一行）用 clientX，否则用 clientY
                var horizontal = false;
                if (nodes.length > 1) {
                    var r0 = nodes[0].getBoundingClientRect();
                    var rn = nodes[nodes.length - 1].getBoundingClientRect();
                    horizontal = Math.abs(r0.top - rn.top) < Math.min(r0.height, 12);
                }
                var idx = nodes.length;
                for (var i = 0; i < nodes.length; i++) {
                    var r = nodes[i].getBoundingClientRect();
                    var hit = horizontal
                        ? (e.clientX < r.left + r.width / 2)
                        : (e.clientY < r.top + r.height / 2);
                    if (hit) { idx = i; break; }
                }
                this._dropIndex = idx;
                this._dropPath = path;
                containerEl.classList.add('lc-drop-target');
                // 占位线位置
                var ph = document.querySelector('.lc-drop-placeholder');
                if (!ph) {
                    ph = document.createElement('div');
                    ph.className = 'lc-drop-placeholder';
                    document.body.appendChild(ph);
                }
                var rect;
                if (nodes.length > 0 && idx >= nodes.length) {
                    var r1 = nodes[nodes.length - 1].getBoundingClientRect();
                    rect = { left: r1.left, top: r1.bottom - 3, width: r1.width };
                } else if (nodes.length > 0) {
                    var r2 = nodes[idx].getBoundingClientRect();
                    rect = { left: r2.left, top: r2.top - 3, width: r2.width };
                } else {
                    var r3 = containerEl.getBoundingClientRect();
                    rect = { left: r3.left + 8, top: r3.top + r3.height / 2 - 3, width: r3.width - 16 };
                }
                ph.style.cssText = 'position:fixed;left:' + rect.left + 'px;top:' + rect.top + 'px;width:' + rect.width + 'px;height:6px;background:#409eff;border-radius:3px;z-index:9998;pointer-events:none;box-shadow:0 0 8px rgba(64,158,255,.7);';
            },
            onDragLeaveNode: function (e) {
                var self = this;
                var related = e.relatedTarget;
                var box = e.currentTarget;
                if (related && box && box.contains(related)) return;  // 移入子元素不清除
                if (this.dragLeaveTimer) clearTimeout(this.dragLeaveTimer);
                this.dragLeaveTimer = setTimeout(function () {
                    self.clearPlaceholder();
                }, 80);
            },
            clearPlaceholder: function () {
                var ph = document.querySelector('.lc-drop-placeholder');
                if (ph) ph.remove();
                document.querySelectorAll('.lc-drop-target').forEach(function (el) { el.classList.remove('lc-drop-target'); });
            },
            onDropInto: function (path, jc, e) {
                // 仅容器可接收 drop；拖到非容器节点（如 LabelWrapper）时忽略，避免组件被移入不渲染的 childrenctrls
                if (!jc || !isContainer(jc.component)) {
                    this._dropIndex = undefined;
                    this._dropPath = null;
                    this.clearPlaceholder();
                    this.removeGhost();
                    return;
                }
                if (this.isLocked(path)) return;
                var fromPath = e.dataTransfer.getData('lc-path');
                var compName = e.dataTransfer.getData('lc-component');
                var targetArr = jc.childrenctrls = jc.childrenctrls || [];
                // 插入索引：优先用最近 dragover 计算的位置
                var idx = (this._dropIndex !== undefined && this._dropPath === path)
                    ? this._dropIndex : targetArr.length;
                if (idx < 0) idx = 0;
                if (idx > targetArr.length) idx = targetArr.length;
                if (fromPath) {
                    var node = this.nodeAt(fromPath);
                    if (!node || fromPath === path) return;
                    // 同父移动：记录原索引，删除后修正插入位置
                    var sameParent = fromPath.indexOf(path + '.childrenctrls[') === 0;
                    var fromIdx = -1;
                    if (sameParent) {
                        var m = fromPath.match(/childrenctrls\[(\d+)\]$/);
                        if (m) fromIdx = parseInt(m[1], 10);
                    }
                    this.onRemove(fromPath);
                    if (sameParent && fromIdx >= 0 && fromIdx < idx) idx--;
                    targetArr.splice(idx, 0, node);
                    this.selectedPath = childPath(path, 'childrenctrls[' + idx + ']');
                    this.refreshTree();
                } else if (compName) {
                    var newNode = blankNode(compName);
                    targetArr.splice(idx, 0, newNode);
                    this.selectedPath = childPath(path, 'childrenctrls[' + idx + ']');
                    this.refreshTree();
                    window.VueLib.eventBus.emit('lc:dropin', { path: path, component: compName });
                }
                this._dropIndex = undefined;
                this._dropPath = null;
                this.clearPlaceholder();
                this.removeGhost();
            },
            /* Sortable 排序结束：容器内重排 / 跨容器移动（同步 childrenctrls） */
            onSortEnd: function (path, evt) {
                var jc = path === 'root' ? this.configObj : Utils.getByPath(this.configObj, path);
                if (!jc || !isContainer(jc.component)) return;
                var arr = jc.childrenctrls = jc.childrenctrls || [];
                if (evt.from === evt.to) {
                    // 容器内排序
                    var o = evt.oldIndex, n = evt.newIndex;
                    if (o === n) { this.refreshTree(); return; }
                    if (o < 0 || o >= arr.length) { this.refreshTree(); return; }
                    var t = arr.splice(o, 1)[0];
                    var ni = n; if (ni > arr.length) ni = arr.length;
                    if (ni < 0) ni = 0;
                    arr.splice(ni, 0, t);
                    // 选中跟随：被移动节点 / 同容器其他节点索引偏移
                    if (this.selectedPath) {
                        var movedOld = childPath(path, 'childrenctrls[' + o + ']');
                        var movedNew = childPath(path, 'childrenctrls[' + ni + ']');
                        if (this.selectedPath === movedOld) {
                            this.selectedPath = movedNew;
                        } else if (this.selectedPath.indexOf(movedOld + '.') === 0) {
                            this.selectedPath = movedNew + this.selectedPath.slice(movedOld.length);
                        } else {
                            var prefix = path === 'root' ? '' : path.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\.';
                            var selRe = new RegExp('^' + prefix + 'childrenctrls\\[(\\d+)\\]');
                            var mm = selRe.exec(this.selectedPath);
                            if (mm) {
                                var selIdx = parseInt(mm[1]);
                                var newSelIdx = selIdx;
                                if (o < ni) { if (selIdx > o && selIdx <= ni) newSelIdx = selIdx - 1; }
                                else { if (selIdx >= ni && selIdx < o) newSelIdx = selIdx + 1; }
                                if (newSelIdx !== selIdx) {
                                    this.selectedPath = this.selectedPath.replace('childrenctrls[' + selIdx + ']', 'childrenctrls[' + newSelIdx + ']');
                                }
                            }
                        }
                    }
                } else {
                    // 跨容器：从 from 容器数组移除，插入 to 容器数组
                    var fromPath = evt.from && evt.from.getAttribute ? evt.from.getAttribute('data-lc-path') : null;
                    var toPath = evt.to && evt.to.getAttribute ? evt.to.getAttribute('data-lc-path') : null;
                    var fromJc = fromPath ? (fromPath === 'root' ? this.configObj : Utils.getByPath(this.configObj, fromPath)) : null;
                    var toJc = toPath ? (toPath === 'root' ? this.configObj : Utils.getByPath(this.configObj, toPath)) : null;
                    if (!fromJc || !toJc || fromJc === toJc) { this.refreshTree(); return; }
                    var fromArr = fromJc.childrenctrls || [];
                    var item = fromArr.splice(evt.oldIndex, 1)[0];
                    if (item) {
                        var toArr = toJc.childrenctrls = toJc.childrenctrls || [];
                        var tni = evt.newIndex; if (tni > toArr.length) tni = toArr.length; if (tni < 0) tni = 0;
                        toArr.splice(tni, 0, item);
                        this.selectedPath = childPath(toPath, 'childrenctrls[' + tni + ']');
                    }
                }
                this.refreshTree();
                window.VueLib.eventBus.emit('lc:tree-refresh');
            },
            /* Palette 原生拖入容器（MIME: application/x-lc-comp） */
            onAddComponent: function (path, name, index) {
                var jc = path === 'root' ? this.configObj : Utils.getByPath(this.configObj, path);
                if (!jc || !isContainer(jc.component)) return;
                jc.childrenctrls = jc.childrenctrls || [];
                var node = blankNode(name);
                var idx = index === undefined || index === null ? jc.childrenctrls.length : index;
                if (idx < 0) idx = 0;
                if (idx > jc.childrenctrls.length) idx = jc.childrenctrls.length;
                jc.childrenctrls.splice(idx, 0, node);
                this.selectedPath = childPath(path, 'childrenctrls[' + idx + ']');
                this.refreshTree();
                this.removeGhost();
                this.clearPlaceholder();
                window.VueLib.eventBus.emit('lc:dropin', { path: path, component: name });
            },

            /* ============ 页面加载/保存 ============ */
            async loadPage(code) {
                var page = await window.VueLib.services.page.getPage(code);
                this.pageCode = page.code;
                this.pageName = page.name;
                this.configObj = JSON.parse(JSON.stringify(page.config));
                this.modelObj = JSON.parse(JSON.stringify(page.model));
                this.ensureDefaults(this.configObj);
                window.VueLib.services.component.setPageContext({ versions: page.versions, model: this.modelObj });
                this.selectedPath = null;
                this.loading = false;
                this.refreshTree();
                window.VueLib.eventBus.emit('lc:loaded', page);
            },
            newPage: function () {
                if (!confirm('新建页面将清空当前画布，继续？')) return;
                this.pageCode = null;
                this.pageName = '';
                this.configObj = blankNode('DivContainer');
                this.modelObj = {};
                this.selectedPath = null;
                this.refreshTree();
            },
            newPageSilent: function () {
                this.pageCode = null;
                this.pageName = '';
                this.configObj = blankNode('DivContainer');
                this.modelObj = {};
                this.selectedPath = null;
            },
            async savePage() {
                if (!this.pageCode) {
                    var code = prompt('请输入页面编码（英文/数字）', 'new-page-' + Date.now().toString(36));
                    if (!code) return;
                    this.pageCode = code.trim();
                }
                if (!this.pageName) this.pageName = this.pageCode;
                this.saving = true;
                try {
                    var res = await window.VueLib.services.page.savePage({
                        code: this.pageCode,
                        name: this.pageName,
                        config: JSON.parse(JSON.stringify(this.configObj)),
                        model: JSON.parse(JSON.stringify(this.modelObj)),
                        componentVersions: (this.configObj.extendinfo && this.configObj.extendinfo.componentVersions) || {}
                    });
                    if (res.success) {
                        this.$message.success('页面已保存: ' + this.pageCode);
                        window.VueLib.eventBus.emit('lc:saved', this.pageCode);
                    } else {
                        this.$message.error(res.message || '保存失败');
                    }
                } finally {
                    this.saving = false;
                }
            },
            preview: function () {
                if (!this.pageCode) { this.$message.warning('请先保存页面'); return; }
                window.open('/#/page/' + this.pageCode, '_blank');
            },
            async refreshComponents() {
                this.$message.info('正在刷新组件注册…');
                try {
                    await fetch('/api/lowcode/component/refresh', { method: 'POST' });
                    await window.VueLib.services.component.refreshMeta();
                    var app = this.$.appContext.app;
                    window.VueLib.services.component.resetApp(app);
                    await window.VueLib.services.component.installComponents(app, await window.VueLib.services.component.ensureMeta());
                    await this.initMeta();
                    this.$message.success('组件刷新完成');
                } catch (e) {
                    this.$message.error('刷新失败: ' + e.message);
                }
            },

            /* ============ 组件树 ============ */
            refreshTree: function () {
                var self = this;
                function convert(node, path) {
                    if (!node || !node.component) return null;
                    var label = node.component +
                        ((node.options && node.options.labeloptions && node.options.labeloptions.label) ? ' - ' + node.options.labeloptions.label : '');
                    var item = { label: label, path: path, iconName: node.component, children: [] };
                    ((node.childrenctrls) || []).forEach(function (c, i) {
                        var child = convert(c, childPath(path, 'childrenctrls[' + i + ']'));
                        if (child) item.children.push(child);
                    });
                    Object.keys(node.slots || {}).forEach(function (k) {
                        var slot = node.slots[k] || {};
                        item.children.push({ label: '◆ ' + k + (slot.open ? ' (开放)' : ''), path: path + '.slots.' + k, iconName: 'Slot', children: [] });
                        (slot.childrenctrls || []).forEach(function (c, i) {
                            var child = convert(c, childPath(path, 'slots.' + k + '.childrenctrls[' + i + ']'));
                            if (child) item.children.push(child);
                        });
                    });
                    return item;
                }
                this.treeData = [convert(this.configObj, 'root')].filter(Boolean);
            },
            onTreeSelect: function (data) {
                if (data && data.path) this.selectedPath = data.path;
            },
            /* 组件树自定义拖拽（HTML5 DnD，不依赖 el-tree 内部 draggable） */
            onTreeDragStart: function (path, e) {
                if (path === 'root') { e.preventDefault(); return; }
                e.dataTransfer.setData('lc-treepath', path);
                e.dataTransfer.effectAllowed = 'move';
            },
            onTreeDragOver: function (path, e) {
                e.dataTransfer.dropEffect = 'move';
            },
            onTreeDrop: function (targetPath, e) {
                var fromPath = e.dataTransfer.getData('lc-treepath');
                if (!fromPath || fromPath === targetPath || fromPath === 'root') return;
                var targetNode = targetPath === 'root' ? this.configObj : this.nodeAt(targetPath);
                if (targetNode && isContainer(targetNode.component)) {
                    // 拖到容器上 → 放入容器末尾
                    this.moveNodeTo(fromPath, targetPath, (targetNode.childrenctrls || []).length);
                } else {
                    // 拖到非容器节点 → 放到它后面（after）
                    if (targetPath === 'root') return;
                    var tp = parentOf(targetPath);
                    var mt = ARR_RE.exec(lastKey(targetPath));
                    if (!mt) return;
                    var tParent = tp === 'root' ? this.configObj : this.nodeAt(tp);
                    if (!tParent) return;
                    var tArr = mt[1].indexOf('slots') === 0 ? Utils.getByPath(tParent, mt[1]) : tParent.childrenctrls;
                    if (!tArr) return;
                    this.moveNodeTo(fromPath, tp, parseInt(mt[2]) + 1);
                }
                this.refreshTree();
            },
            /* 通用移动节点：从 fromPath 移入 toParentPath.childrenctrls[toIndex]（支持 slots 数组） */
            moveNodeTo: function (fromPath, toParentPath, toIndex) {
                if (fromPath === 'root') return false;
                var fromParent = this.parentNodeOf(fromPath);
                var mf = ARR_RE.exec(lastKey(fromPath));
                if (!mf || !fromParent) return false;
                var fromArr = mf[1].indexOf('slots') === 0 ? Utils.getByPath(fromParent, mf[1]) : fromParent.childrenctrls;
                var fromIdx = parseInt(mf[2]);
                var node = fromArr && fromArr[fromIdx];
                if (!node) return false;
                var toParent = toParentPath === 'root' ? this.configObj : this.nodeAt(toParentPath);
                if (!toParent) return false;
                // 禁止移入自身或其子孙
                if (toParentPath !== 'root' && (fromPath === toParentPath || fromPath.indexOf(toParentPath + '.') === 0)) return false;
                var toArr = toParent.childrenctrls = toParent.childrenctrls || [];
                if (toArr === fromArr && fromIdx < toIndex) toIndex--;
                fromArr.splice(fromIdx, 1);
                if (toIndex < 0) toIndex = 0;
                if (toIndex > toArr.length) toIndex = toArr.length;
                toArr.splice(toIndex, 0, node);
                this.selectedPath = childPath(toParentPath, 'childrenctrls[' + toIndex + ']');
                this.refreshTree();
                return true;
            },

            /* ============ 属性面板 ============ */
            addComOption: function () {
                var key = prompt('新选项 key（如 placeholder）');
                if (!key) return;
                this.ensureDefaults(this.selectedNode);
                var opts = this.selectedNode.options.comoptions;
                if (key in opts) { this.$message.warning('key 已存在'); return; }
                opts[key] = '';
            },
            removeComOption: function (key) {
                if (confirm('删除选项 ' + key + '？')) delete this.selectedNode.options.comoptions[key];
            },
            optionType: function (v) {
                if (v === null || v === undefined) return 'text';
                if (typeof v === 'boolean') return 'switch';
                if (typeof v === 'number') return 'number';
                if (typeof v === 'object') return 'json';
                return 'text';
            },
            setOptionValue: function (key, val) {
                var opts = this.selectedNode.options.comoptions;
                if (typeof opts[key] === 'boolean') opts[key] = val === true || val === 'true';
                else if (typeof opts[key] === 'number') opts[key] = Number(val) || 0;
                else opts[key] = val;
            },
            applyJsonOption: function (key, text) {
                try {
                    this.selectedNode.options.comoptions[key] = JSON.parse(text);
                } catch (e) {
                    // 输入过程中可能暂时不合法，静默保持旧值；失焦时若仍不合法再提示
                    if (window.__DYN_DEBUG) console.warn('[Dyn] JSON 选项暂不合法:', key, text);
                }
            },
            addValidator: function () {
                this.ensureDefaults(this.selectedNode);
                this.selectedNode.validators.push({ type: 'required', message: '', value: null });
            },
            removeValidator: function (i) { this.selectedNode.validators.splice(i, 1); },
            addSlot: function () {
                this.ensureDefaults(this.selectedNode);
                var key = prompt('插槽名（如 body）');
                if (!key) return;
                if (this.selectedNode.slots[key]) { this.$message.warning('插槽已存在'); return; }
                this.selectedNode.slots[key] = { label: key, open: false, childrenctrls: [] };
            },
            removeSlot: function (key) {
                if (confirm('删除插槽 ' + key + '？')) delete this.selectedNode.slots[key];
            },

            /* ============ 组合组件 ============ */
            openCompositeDlg: function () {
                var n = this.selectedNode;
                if (!n) { this.$message.warning('请先选中要封装为组合组件的节点'); return; }
                if (n === this.configObj) { this.$message.warning('不能把整页作为组合组件（请选中页面内子组件）'); return; }
                var meta = this.metaMap[n.component];
                this.compositeCfg = {
                    name: (n.component || 'Com') + 'X',
                    displayName: n.component,
                    icon: meta ? meta.icon : 'Box',
                    openProps: Object.keys(n.options.comoptions || {}).map(function (k) {
                        return { key: k, checked: true };
                    }),
                    openSlots: Object.keys(n.slots || {}).map(function (k) {
                        return { key: k, checked: true, label: (n.slots[k] || {}).label || k };
                    }),
                    node: n
                };
                this.showCompositeDlg = true;
            },
            async saveComposite() {
                var cfg = this.compositeCfg;
                if (!cfg.name.trim()) { this.$message.warning('请输入组件名'); return; }
                var node = JSON.parse(JSON.stringify(cfg.node));
                node.extendinfo = node.extendinfo || {};
                node.extendinfo.compositeMeta = {
                    openProps: cfg.openProps.filter(function (p) { return p.checked; }).map(function (p) { return p.key; }),
                    openSlots: cfg.openSlots.filter(function (s) { return s.checked; }).map(function (s) { return s.key; })
                };
                var schema = {
                    openProps: cfg.openProps.filter(function (p) { return p.checked; }).map(function (p) {
                        return { key: p.key, label: p.key, type: 'text', group: 'comoptions' };
                    }),
                    openSlots: cfg.openSlots.filter(function (s) { return s.checked; }).map(function (s) {
                        return { key: s.key, label: s.label };
                    })
                };
                var res = await fetch('/api/lowcode/component', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        name: cfg.name.trim(),
                        displayName: cfg.displayName,
                        icon: cfg.icon,
                        compositeConfigJson: JSON.stringify(node),
                        configSchemaJson: JSON.stringify(schema)
                    })
                });
                var json = await res.json();
                if (json.success) {
                    this.$message.success('组合组件已保存');
                    this.showCompositeDlg = false;
                    await window.VueLib.services.component.refreshMeta();
                    var app = this.$.appContext.app;
                    window.VueLib.services.component.resetApp(app);
                    await window.VueLib.services.component.installComponents(app, await window.VueLib.services.component.ensureMeta());
                    await this.initMeta();
                } else {
                    this.$message.error(json.message || '保存失败');
                }
            },

            /* ============ 版本锁定 ============ */
            setLockedVersion: function (row, version) {
                this.configObj.extendinfo = this.configObj.extendinfo || {};
                this.configObj.extendinfo.componentVersions = this.configObj.extendinfo.componentVersions || {};
                if (version > 0) this.configObj.extendinfo.componentVersions[row.name] = version;
                else delete this.configObj.extendinfo.componentVersions[row.name];
                window.VueLib.services.component.setPageContext({
                    versions: this.configObj.extendinfo.componentVersions,
                    model: this.modelObj
                });
            },

            /* ============ JSON / Model ============ */
            applyJson: function () {
                try {
                    this.configObj = JSON.parse(this.jsonText);
                    this.ensureDefaults(this.configObj);
                    this.refreshTree();
                    this.showJson = false;
                    this.$message.success('JSON 已应用');
                } catch (e) {
                    this.$message.error('JSON 解析失败: ' + e.message);
                }
            },
            applyModel: function () {
                try {
                    this.modelObj = JSON.parse(this.modelText);
                    this.showModel = false;
                    this.$message.success('Model 已应用');
                } catch (e) {
                    this.$message.error('JSON 解析失败: ' + e.message);
                }
            },

            /* ============ 初始化 ============ */
            async initMeta() {
                var list = await window.VueLib.services.component.ensureMeta();
                this.metaList = list;
                this.metaMap = {};
                var self = this;
                list.forEach(function (m) { self.metaMap[m.name] = m; });
            }
        },
        async mounted() {
            var self = this;
            console.log('[Designer] mounted start');
            try {
            window.VueLib.designer.isContainer = isContainer;
            await this.initMeta();
            // 注册动态组件到设计器应用
            var app = this.$.appContext.app;
            window.VueLib.services.component.resetApp(app);
            await window.VueLib.services.component.installComponents(app, this.metaList);

            var pageCode = this.page;
            if (!pageCode) {
                var q = new URLSearchParams(location.search);
                pageCode = q.get('page');
            }
            if (pageCode) await this.loadPage(pageCode);
            else this.newPageSilent();
            window.VueLib.eventBus.on('lc:tree-refresh', this.refreshTree);
            this.loading = false;
            console.log('[Designer] mounted done', pageCode, this.configObj && this.configObj.component, (this.configObj && this.configObj.childrenctrls || []).length);
            } catch (e) {
                console.error('[Designer] mounted ERROR:', e && e.stack || e);
                this.loading = false;
            }
        },
        beforeUnmount: function () {
            window.VueLib.eventBus.off('lc:tree-refresh', this.refreshTree);
        },
        template: '' +
            '<div class="lc-designer h-screen flex flex-col">' +
            /* ===== 顶部 ===== */
            '  <div class="lc-toolbar flex items-center gap-2 px-3 py-2 bg-white border-b border-gray-200">' +
            '    <span class="font-bold text-gray-800 mr-1">VueLib 设计器</span>' +
            '    <el-input v-model="pageCode" placeholder="页面编码" class="!w-40" size="small"></el-input>' +
            '    <el-input v-model="pageName" placeholder="页面名称" class="!w-40" size="small"></el-input>' +
            '    <el-button type="primary" size="small" :loading="saving" @click="savePage">保存</el-button>' +
            '    <el-button size="small" @click="newPage">新建</el-button>' +
            '    <el-button size="small" @click="preview" :disabled="!pageCode">预览</el-button>' +
            '    <el-button size="small" @click="refreshComponents">刷新组件</el-button>' +
            '    <el-button size="small" @click="showJson = !showJson">JSON</el-button>' +
            '    <el-button size="small" @click="showModel = !showModel">Model</el-button>' +
            '    <el-button size="small" type="warning" plain @click="openCompositeDlg" :disabled="!selectedNode || selectedPath === \'root\'">封装组合组件</el-button>' +
            '  </div>' +
            /* ===== 主体 ===== */
            '  <div class="flex flex-1 min-h-0 relative">' +
            /* ---- 左：组件库 + 树（可折叠） ---- */
            '    <div v-if="!paletteCollapsed" class="lc-left-panel w-64 border-r border-gray-200 bg-white flex flex-col min-h-0">' +
            '      <div class="flex items-center justify-between px-2 py-1.5 border-b border-gray-100">' +
            '        <span class="text-xs text-gray-500">组件库</span>' +
            '        <button class="lc-panel-toggle" title="折叠左侧面板" @click="paletteCollapsed = true">«</button>' +
            '      </div>' +
            '      <el-tabs class="lc-left-tabs flex-1 min-h-0">' +
            '        <el-tab-pane label="组件库">' +
            '          <div class="px-2 pb-2 pt-1">' +
            '            <el-radio-group v-model="categoryFilter" size="small" class="lc-cat-radio">' +
            '              <el-radio-button v-for="c in categories" :key="c" :label="c">{{ c }}</el-radio-button>' +
            '            </el-radio-group>' +
            '          </div>' +
            '          <div class="px-2 pb-4 overflow-auto" style="height:calc(100vh - 210px)">' +
            '            <div v-for="g in paletteGroups" :key="g.category" class="mb-3">' +
            '              <div class="lc-cat-title" @click="collapsedCats[g.category] = !collapsedCats[g.category]">' +
            '                <span class="lc-cat-arrow" :class="{ open: !collapsedCats[g.category] }">▸</span>' +
            '                <span class="lc-cat-name">{{ g.category }}</span>' +
            '                <span class="lc-cat-count">{{ g.items.length }}</span>' +
            '              </div>' +
            '              <div v-show="!collapsedCats[g.category]" class="lc-palette-grid">' +
            '                <div v-for="m in g.items" :key="m.name" draggable="true" class="lc-palette-item" :data-comp="m.name" :title="m.name" @dragstart="onPaletteDrag($event, m.name)" @dragend="onPaletteDragEnd" @click="onPaletteClick(m.name)">' +
            '                  <span class="lc-palette-icon">{{ lcIcon(m.name) }}</span>' +
            '                  <span class="lc-palette-label">{{ m.displayName }}</span>' +
            '                </div>' +
            '              </div>' +
            '            </div>' +
            '          </div>' +
            '        </el-tab-pane>' +
            '        <el-tab-pane label="组件树">' +
            '          <el-tree :data="treeData" node-key="path" default-expand-all highlight-current @current-change="onTreeSelect" class="p-2 text-sm">' +
            '            <template #default="{ data }">' +
            '              <span class="lc-tree-node" draggable="true" @dragstart.stop="onTreeDragStart(data.path, $event)" @dragover.stop.prevent="onTreeDragOver(data.path, $event)" @drop.stop="onTreeDrop(data.path, $event)" :title="data.path">' +
            '                <span class="lc-tree-icon">{{ data.iconName ? lcIcon(data.iconName) : "🧩" }}</span>' +
            '                <span class="truncate flex-1 min-w-0">{{ data.label }}</span>' +
            '                <span class="lc-tree-ops" v-if="data.path && data.path !== \'root\'">' +
            '                  <button class="lc-tree-btn" @click.stop="onMoveUp(data.path)" title="上移">↑</button>' +
            '                  <button class="lc-tree-btn" @click.stop="onMoveDown(data.path)" title="下移">↓</button>' +
            '                </span>' +
            '              </span>' +
            '            </template>' +
            '          </el-tree>' +
            '        </el-tab-pane>' +
            '      </el-tabs>' +
            '    </div>' +
            '    <div v-else class="lc-left-collapsed" @click="paletteCollapsed = false" title="展开组件库">' +
            '      <span>库</span>' +
            '    </div>' +
            /* ---- 中：画布 ---- */
            '    <div class="flex-1 overflow-auto bg-gray-100 p-4">' +
            '      <div class="bg-white border border-gray-200 rounded shadow-sm p-6 min-h-full">' +
            '        <lc-node v-if="configObj && configObj.component && !loading"' +
            '          :jsonconfig="configObj" :parentmodelinfo="modelObj" :design="true" :design-api="designApi()" node-path="root" :depth="0"></lc-node>' +
            '        <div v-else class="text-gray-400 text-sm p-10">加载中…</div>' +
            '      </div>' +
            '    </div>' +
            /* ---- 右：属性面板 ---- */
            '    <div class="w-80 border-l border-gray-200 bg-white overflow-auto" style="height:calc(100vh - 48px)">' +
            '      <div v-if="!selectedNode" class="p-6 text-gray-400 text-sm">点击画布中的组件进行属性编辑</div>' +
            '      <div v-else class="p-3 space-y-4">' +
            '        <div class="text-sm font-bold border-b border-gray-100 pb-2">属性 - {{ selectedNode.component }}</div>' +

            '        <div class="lc-panel">' +
            '          <div class="lc-panel-title">基础</div>' +
            '          <div class="lc-field"><label>组件</label><el-input :model-value="selectedNode.component" size="small" disabled></el-input></div>' +
            '          <div class="lc-field"><label>modelname</label><el-input v-model="selectedNode.modelname" size="small" placeholder="如 user.name / ~abs.name / arr[0]"></el-input></div>' +
            '          <div class="lc-field"><label>标签</label><el-input v-model="selectedNode.options.labeloptions.label" size="small"></el-input></div>' +
            '          <div class="lc-field"><label>标签宽度</label><el-input v-model="selectedNode.options.labeloptions.labelWidth" size="small" placeholder="如 120px / 8em / auto"></el-input></div>' +
            '          <div class="lc-field"><label>标签对齐</label>' +
            '            <el-select v-model="selectedNode.options.labeloptions.labelPosition" size="small" clearable placeholder="继承容器">' +
            '              <el-option label="左对齐" value="left"></el-option>' +
            '              <el-option label="右对齐" value="right"></el-option>' +
            '            </el-select></div>' +
            '          <div class="lc-field flex items-center gap-4">' +
            '            <label>必填</label><el-switch v-model="selectedNode.options.labeloptions.required" size="small"></el-switch>' +
            '            <label>显示标签</label><el-switch v-model="selectedNode.options.labeloptions.show" size="small"></el-switch>' +
            '          </div>' +
            '          <div class="lc-field"><label>class(Tailwind)</label><el-input v-model="selectedNode.options.itemoptions.class" size="small"></el-input></div>' +
            '          <div class="lc-field"><label>style(JSON)</label><el-input v-model="itemStyleText" size="small" type="textarea" :rows="2"></el-input></div>' +
            '        </div>' +

            '        <div class="lc-panel" v-if="selectedNode.component === \'DivContainer\'">' +
            '          <div class="lc-panel-title">Flexbox 生成器（Tailwind）</div>' +
            '          <div class="lc-field"><label>启用</label><el-switch v-model="flexboxCfg.enabled" size="small"></el-switch></div>' +
            '          <div class="lc-field"><label>方向</label>' +
            '            <el-radio-group v-model="flexboxCfg.direction" size="small">' +
            '              <el-radio-button label="row">横向</el-radio-button><el-radio-button label="col">纵向</el-radio-button>' +
            '            </el-radio-group></div>' +
            '          <div class="lc-field"><label>主轴对齐</label>' +
            '            <el-select v-model="flexboxCfg.justify" size="small">' +
            '              <el-option v-for="j in [\'start\',\'end\',\'center\',\'between\',\'around\',\'evenly\']" :key="j" :label="j" :value="j"></el-option>' +
            '            </el-select></div>' +
            '          <div class="lc-field"><label>交叉轴</label>' +
            '            <el-select v-model="flexboxCfg.align" size="small">' +
            '              <el-option v-for="a in [\'start\',\'end\',\'center\',\'stretch\']" :key="a" :label="a" :value="a"></el-option>' +
            '            </el-select></div>' +
            '          <div class="lc-field"><label>间距 gap: {{ flexboxCfg.gap }}</label>' +
            '            <el-slider v-model="flexboxCfg.gap" :min="0" :max="24"></el-slider></div>' +
            '          <div class="lc-field"><label>额外 class</label><el-input v-model="flexboxCfg.className" size="small"></el-input></div>' +
            '          <div class="text-[10px] text-gray-400 bg-gray-50 p-1 rounded break-all">生成：{{ flexPreview }}</div>' +
            '        </div>' +

            '        <div class="lc-panel" v-if="selectedNode.component === \'DivContainer\'">' +
            '          <div class="lc-panel-title">容器标签布局（容器内全部表单项生效，单项可覆盖）</div>' +
            '          <div class="lc-field"><label>标签宽度</label><el-input v-model="formLayoutCfg.labelWidth" size="small" placeholder="如 120px / 8em（空 = auto）"></el-input></div>' +
            '          <div class="lc-field"><label>标签对齐</label>' +
            '            <el-select v-model="formLayoutCfg.labelPosition" size="small" clearable placeholder="默认左对齐">' +
            '              <el-option label="左对齐" value="left"></el-option>' +
            '              <el-option label="右对齐" value="right"></el-option>' +
            '            </el-select></div>' +
            '          <div class="text-[10px] text-gray-400">子表单项在"基础"面板单独设置宽度/对齐可覆盖此处。</div>' +
            '        </div>' +

            '        <div class="lc-panel">' +
            '          <div class="lc-panel-title">组件选项 <el-button link type="primary" size="small" @click="addComOption">+ 添加</el-button></div>' +
            '          <div v-for="(v, k) in selectedNode.options.comoptions" :key="k" class="lc-field">' +
            '            <label class="flex items-center gap-1">{{ k }} <span class="text-red-500 cursor-pointer" @click="removeComOption(k)">×</span></label>' +
            '            <el-switch v-if="optionType(v) === \'switch\'" v-model="selectedNode.options.comoptions[k]" size="small"></el-switch>' +
            '            <el-input-number v-else-if="optionType(v) === \'number\'" v-model="selectedNode.options.comoptions[k]" size="small"></el-input-number>' +
            '            <el-input v-else-if="optionType(v) === \'text\'" v-model="selectedNode.options.comoptions[k]" size="small"></el-input>' +
            '            <el-input v-else :model-value="JSON.stringify(v)" size="small" type="textarea" :rows="2" @input="applyJsonOption(k, $event)"></el-input>' +
            '          </div>' +
            '        </div>' +

            '        <div class="lc-panel">' +
            '          <div class="lc-panel-title">校验规则 <el-button link type="primary" size="small" @click="addValidator">+ 添加</el-button></div>' +
            '          <div v-for="(rule, i) in selectedNode.validators" :key="i" class="lc-field">' +
            '            <div class="flex gap-1 items-center">' +
            '              <el-select v-model="rule.type" size="small" style="width:110px">' +
            '                <el-option v-for="t in [\'required\',\'requiredTrue\',\'minLength\',\'maxLength\',\'min\',\'max\',\'pattern\',\'email\',\'phone\',\'url\',\'number\']" :key="t" :label="t" :value="t"></el-option>' +
            '              </el-select>' +
            '              <el-input v-model="rule.value" size="small" placeholder="值"></el-input>' +
            '              <span class="text-red-500 cursor-pointer" @click="removeValidator(i)">×</span>' +
            '            </div>' +
            '            <el-input v-model="rule.message" size="small" placeholder="错误提示消息" class="mt-1"></el-input>' +
            '          </div>' +
            '        </div>' +

            '        <div class="lc-panel">' +
            '          <div class="lc-panel-title">插槽 <el-button link type="primary" size="small" @click="addSlot">+ 添加</el-button></div>' +
            '          <div v-for="(s, key) in selectedNode.slots" :key="key" class="lc-field">' +
            '            <div class="flex items-center justify-between text-xs">' +
            '              <span>{{ key }}（{{ s.childrenctrls ? s.childrenctrls.length : 0 }} 子项）</span>' +
            '              <span class="flex items-center gap-1">开放<el-switch v-model="s.open" size="mini"></el-switch>' +
            '              <span class="text-red-500 cursor-pointer" @click="removeSlot(key)">×</span></span>' +
            '            </div>' +
            '          </div>' +
            '          <div v-if="!Object.keys(selectedNode.slots || {}).length" class="text-[10px] text-gray-400">无插槽（Tabs/Slot/组合组件使用）</div>' +
            '        </div>' +

            '        <div class="lc-panel">' +
            '          <div class="lc-panel-title">动作链 extendinfo.actions.chain（JSON）</div>' +
            '          <el-input v-model="chainText" size="small" type="textarea" :rows="6" class="font-mono" placeholder="[{&quot;action&quot;:&quot;notify&quot;,&quot;options&quot;:{...}}]"></el-input>' +
            '        </div>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +

            /* ===== JSON / Model 抽屉 ===== */
            '  <el-drawer v-model="showJson" title="页面 JSON" size="45%">' +
            '    <el-input v-model="jsonText" type="textarea" :rows="20" class="w-full font-mono"></el-input>' +
            '    <div class="mt-2 flex gap-2"><el-button type="primary" size="small" @click="applyJson">应用</el-button></div>' +
            '  </el-drawer>' +
            '  <el-drawer v-model="showModel" title="页面 Model" size="45%">' +
            '    <el-input v-model="modelText" type="textarea" :rows="20" class="w-full font-mono"></el-input>' +
            '    <div class="mt-2 flex gap-2"><el-button type="primary" size="small" @click="applyModel">应用</el-button></div>' +
            '  </el-drawer>' +

            /* ===== 组合组件对话框 ===== */
            '  <el-dialog v-model="showCompositeDlg" title="封装组合组件" width="560px">' +
            '    <div v-if="compositeCfg" class="space-y-3">' +
            '      <div class="flex gap-2">' +
            '        <el-input v-model="compositeCfg.name" placeholder="组件名(英文)"></el-input>' +
            '        <el-input v-model="compositeCfg.displayName" placeholder="显示名"></el-input>' +
            '      </div>' +
            '      <div class="text-sm"><b>开放属性（外部可配置）</b>' +
            '        <div class="flex flex-wrap gap-2 mt-1">' +
            '          <el-checkbox v-for="p in compositeCfg.openProps" :key="p.key" v-model="p.checked">{{ p.key }}</el-checkbox>' +
            '        </div>' +
            '      </div>' +
            '      <div class="text-sm"><b>开放插槽（外部可拖入子组件）</b>' +
            '        <div class="flex flex-wrap gap-2 mt-1">' +
            '          <el-checkbox v-for="s in compositeCfg.openSlots" :key="s.key" v-model="s.checked">{{ s.label }}</el-checkbox>' +
            '        </div>' +
            '      </div>' +
            '    </div>' +
            '    <template #footer><el-button @click="showCompositeDlg=false">取消</el-button><el-button type="primary" @click="saveComposite">保存</el-button></template>' +
            '  </el-dialog>'
    };

    /* ============ 挂载入口 ============ */
    var designerApp = null;

    async function mount(el, options) {
        var container = Utils.resolveEl(el);
        if (!container) return null;
        if (designerApp) {
            try { designerApp.unmount(); } catch (e) { /* 忽略 */ }
            container.innerHTML = '';
            designerApp = null;
        }
        var app = Vue.createApp(Designer, { page: (options && options.page) || null });
        app.config.errorHandler = function (err, inst, info) {
            var compName = inst && inst._ && inst._.type ? (inst._.type.name || inst._.type.__name || 'anon') : 'none';
            var msg = (err && err.message ? err.message.slice(0, 120) : String(err));
            if (window.__DYN_DEBUG) document.title = 'LC-ERR:' + compName + ':' + msg;
            try { window.__lcLastError = { comp: compName, msg: msg, stack: String(err && err.stack || err), info: info }; } catch (e) { /* 忽略 */ }
            console.error('[LC render error]', err, info, compName);
        };
        app.use(window.ElementPlus);
        app.component('LcNode', NDynamicCom);
        app.component('LCNode', NDynamicCom);
        window.VueLib._designerApp = app;
        try {
            app.mount(container);
        } catch (e) {
            document.title = 'LC-MOUNT-ERR:' + (e && e.message ? e.message.slice(0, 80) : String(e));
        }
        designerApp = app;
        return app;
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.designer = {
        mount: mount,
        isContainer: isContainer
    };
})();

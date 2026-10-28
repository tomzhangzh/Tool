/**
 * 设计器拖拽封装 —— 基于 vue-draggable-plus 的 useDraggable
 * vue-draggable-plus 内部自动处理 SortableJS 与 Vue 响应式数组的同步，
 * 我们只负责：左侧组件库 clone、拖入时把 metadata 转成实际组件配置、高亮。
 */
(function () {
    var global = window;
var __plugin = {
    name:'designer-sortable', stage:'designer', requires:[],
    setup:function(ctx){
    var DesignerSortable = {};

    function getUseDraggable() {
        if (window.VueDraggablePlus && typeof window.VueDraggablePlus.useDraggable === 'function') {
            return window.VueDraggablePlus.useDraggable;
        }
        return null;
    }

    function genUid() {
        if (window.DynDesignerOps && window.DynDesignerOps.uidOf) {
            var obj = {};
            return window.DynDesignerOps.uidOf(obj);
        }
        return "dyn-" + Math.random().toString(36).slice(2, 10);
    }

    function findMeta(componentName, scope) {
        if (!scope || !scope.componentMetaList) return null;
        for (var i = 0; i < scope.componentMetaList.length; i++) {
            var m = scope.componentMetaList[i];
            var nm = m.ComponentName || m.componentName;
            if (nm === componentName) return m;
        }
        return null;
    }

    // ===== 拖拽视觉状态辅助 =====
    function setDragActive(on) {
        document.body.classList.toggle("dyn-drag-active", !!on);
    }
    function clearDropMarks() {
        document.querySelectorAll(".dyn-container-hover,.dyn-container-denied").forEach(function (el) {
            el.classList.remove("dyn-container-hover");
            el.classList.remove("dyn-container-denied");
        });
    }
    // 被拖组件名：库卡片看 data-meta；画布节点看 data-dyn-uid 反查配置树
    function getDragName(item, scope) {
        if (!item) return null;
        var metaName = item.getAttribute && item.getAttribute("data-meta");
        if (metaName) return metaName;
        var uid = item.getAttribute && item.getAttribute("data-dyn-uid");
        if (uid && scope && scope.pageJson && window.DynDesignerOps) {
            var node = window.DynDesignerOps.findNode(scope.pageJson, uid);
            return node ? node.component : null;
        }
        return null;
    }
    // 目标容器（.dyn-children-wrap）所属组件名
    function getContainerNode(wrap, scope) {
        if (!wrap || !wrap.closest || !scope || !scope.pageJson || !window.DynDesignerOps) return null;
        var host = wrap.closest("[data-dyn-uid]");
        if (!host) return null;
        return window.DynDesignerOps.findNode(scope.pageJson, host.getAttribute("data-dyn-uid"));
    }
    // 判断目标 wrap 当前能否接收被拖组件：类型白名单 + 禁止放进自身/自己的后代
    function canAccept(wrap, item, scope) {
        try {
            var dragName = getDragName(item, scope);
            var targetNode = getContainerNode(wrap, scope);
            if (!dragName || !targetNode || !window.DynDesignerOps) return false;
            if (!window.DynDesignerOps.checkCanDrop(targetNode.component, dragName)) return false;
            var dragUid = item.getAttribute && item.getAttribute("data-dyn-uid");
            if (dragUid) {
                if (targetNode.__uid === dragUid) return false;
                if (window.DynDesignerOps.isDescendantOf &&
                    window.DynDesignerOps.isDescendantOf(targetNode, window.DynDesignerOps.findNode(scope.pageJson, dragUid))) return false;
            }
            return true;
        } catch (e) {
            console.error("[designer-sortable] canAccept error", e);
            return false;
        }
    }

    // SortableJS 的 onMove 始终在“源列表”实例上触发（evt.to 为当前悬停列表），
    // 因此组件库与画布各容器都必须挂载，才能覆盖 库→画布 / 画布间 两种拖拽的校验与高亮。
    function makeOnMove(scope) {
        return function (evt) {
            clearDropMarks();
            var target = evt && evt.to;
            var wrap = target && target.classList && target.classList.contains("dyn-children-wrap")
                ? target
                : (target && target.closest ? target.closest(".dyn-children-wrap") : null);
            if (!wrap) return false;
            var dragged = (evt && (evt.dragged || evt.item)) || null;
            var ok = canAccept(wrap, dragged, scope);
            wrap.classList.add(ok ? "dyn-container-hover" : "dyn-container-denied");
            return ok;
        };
    }

    /**
     * 左侧组件库：clone 模式，sort 关闭
     * @param {HTMLElement} el 卡片容器
     * @param {Array} metaList 组件元数据数组（reactive）
     */
    DesignerSortable.createComponentLibrary = function (el, metaList, scope) {
        var useDraggable = getUseDraggable();
        if (!el || !useDraggable) return null;

        return useDraggable(el, metaList, {
            group: { name: "dyn-designer", pull: "clone", put: false },
            sort: false,
            animation: 0,
            ghostClass: "dyn-library-ghost",
            dragClass: "dyn-library-drag",
            forceFallback: true,
            fallbackClass: "dyn-fallback-drag",
            fallbackTolerance: 3,
            fallbackOnBody: true,
            filter: "input,textarea,button,img",
            preventOnFilter: true,
            onStart: function () { setDragActive(true); },
            // evt.to 为当前悬停的画布容器：库→画布 时 SortableJS 只会回调源（库）实例的 onMove
            onMove: makeOnMove(scope),
            onEnd: function () { setDragActive(false); clearDropMarks(); },
            onUnchoose: function () { setDragActive(false); clearDropMarks(); }
        });
    };

    /**
     * 画布容器：接收 clone + 内部排序，useDraggable 自动同步 childrenctrls 数组
     */
    DesignerSortable.createCanvasContainer = function (listEl, parentCfg, scope, isGrid) {
        var useDraggable = getUseDraggable();
        if (!listEl || !useDraggable || !parentCfg) return null;
        if (!Array.isArray(parentCfg.childrenctrls)) parentCfg.childrenctrls = [];

        return useDraggable(listEl, parentCfg.childrenctrls, {
            // pull:true：允许在画布各容器间移动（同组 + put:true 可互相接收）；左侧库是 clone 不受影响
            group: { name: "dyn-designer", pull: true, put: true },
            sort: true,
            // 不指定 draggable：SortableJS 默认只处理根元素的直接子元素。
            // 嵌套容器场景下，若用 "[data-dyn-uid]" 会匹配到内层容器的子组件，
            // 导致外层 Sortable 拦截内层子组件的拖拽，使其无法拖动。
            emptyInsertThreshold: 40,
            dragoverBubble: true,
            animation: 150,
            ghostClass: "dyn-designer-ghost",
            chosenClass: "dyn-designer-chosen",
            dragClass: "dyn-designer-dragging",
            forceFallback: true,
            fallbackClass: "dyn-fallback-drag",
            fallbackTolerance: 3,
            fallbackOnBody: true,
            filter: "input,textarea,button,img,.dyn-no-drag",
            preventOnFilter: true,
            swapThreshold: 0.65,

            onStart: function (evt) {
                setDragActive(true);
                listEl.classList.add("dyn-dragging-active");
                if (scope) {
                    var name = getDragName(evt && evt.item, scope);
                    if (name) scope.dragging = name;
                }
            },

            onMove: makeOnMove(scope),

            onEnd: function () {
                setDragActive(false);
                listEl.classList.remove("dyn-dragging-active");
                clearDropMarks();
                if (scope) scope.dragging = null;
                if (scope && window.DynDesignerOps) {
                    // 与 ops 统一：DOM 更新完成后重定位选中框，后续尺寸变化由 ResizeObserver 兜底
                    var V = window.Vue;
                    var relayout = function () { window.DynDesignerOps.updateOverlay(scope); };
                    if (V && V.nextTick) V.nextTick(relayout); else setTimeout(relayout, 0);
                }
            },

            onUnchoose: function () {
                setDragActive(false);
                listEl.classList.remove("dyn-dragging-active");
                clearDropMarks();
                if (scope) scope.dragging = null;
            },

            // 从左侧拖入：vue-draggable-plus 已经把 metadata 对象 splice 到 childrenctrls，
            // 我们在这里把它替换成真正的组件配置；任何失败分支都必须回滚，不能把 metadata 留在树里。
            onAdd: function (evt) {
                var newIndex = evt.newIndex;
                if (newIndex == null) newIndex = parentCfg.childrenctrls.length - 1;
                var inserted = parentCfg.childrenctrls[newIndex];
                // 回滚：移除自动插入的库 metadata（它没有 component/options，留着会产出坏配置）
                function rollback() {
                    if (parentCfg.childrenctrls[newIndex] === inserted) {
                        parentCfg.childrenctrls.splice(newIndex, 1);
                    }
                }
                if (!inserted) return;
                try {
                    var componentName = inserted.ComponentName || inserted.componentName ||
                        (evt.item && evt.item.getAttribute && evt.item.getAttribute("data-meta"));
                    if (!componentName) { rollback(); return; }

                    var newCfg;
                    if (componentName === '__composition__') {
                        // 本地组合库卡片：compCfg 是已去内部字段的组件子树模板，整棵重新发号 uid
                        var tpl = inserted.compCfg;
                        if (!tpl) { rollback(); return; }
                        newCfg = JSON.parse(JSON.stringify(tpl));
                        if (window.DynDesignerOps && window.DynDesignerOps.regenerateUids) {
                            window.DynDesignerOps.regenerateUids(newCfg);
                        } else {
                            newCfg.__uid = genUid();
                        }
                    } else {
                        var meta = findMeta(componentName, scope);
                        var defaults = meta && (meta.DefaultConfigJson || meta.defaultConfigJson);

                        if (typeof defaults === "string") newCfg = JSON.parse(defaults);
                        else if (defaults && typeof defaults === "object") newCfg = JSON.parse(JSON.stringify(defaults));
                        else newCfg = { component: componentName, modelname: "", options: { comoptions:{}, labeloptions:{ label:"", show:true }, itemoptions:{ style:{}, class:"" } }, childrenctrls: [], validators: [], slots: {}, extendinfo: {} };

                        newCfg.__uid = genUid();
                    }
                    if (!newCfg.component) newCfg.component = componentName === '__composition__' ? (newCfg.component || 'DynElContainer') : componentName;
                    if (!newCfg.options) newCfg.options = {};

                    if (isGrid) {
                        if (!newCfg.options.itemoptions) newCfg.options.itemoptions = { style: {}, class: "" };
                        var cls = newCfg.options.itemoptions.class || "";
                        if (cls.indexOf("grid-span-2") < 0) newCfg.options.itemoptions.class = (cls + " grid-span-2").trim();
                    }

                    // 替换自动插入的 metadata 对象
                    parentCfg.childrenctrls.splice(newIndex, 1, newCfg);

                    if (scope && window.DynDesignerOps) window.DynDesignerOps.select(scope, newCfg);
                } catch (e) {
                    console.error("[useDraggable onAdd] 组件转换失败，已回滚拖入", e);
                    rollback();
                }
            }
        });
    };

    DesignerSortable.destroy = function (ins) {
        // 拖拽进行中卸载时也要清掉 body 态，否则 cursor/禁用样式会残留到下个页面
        setDragActive(false);
        document.body.classList.remove("dyn-dragging-active");
        clearDropMarks();
        if (ins && typeof ins.destroy === "function") { try { ins.destroy(); } catch (e) {} }
    };

    window.DesignerSortable = DesignerSortable;
    }
};
if(window.DynKernel) window.DynKernel.register(__plugin);
else (window.__DYN_KERNEL_PENDING__=window.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})();

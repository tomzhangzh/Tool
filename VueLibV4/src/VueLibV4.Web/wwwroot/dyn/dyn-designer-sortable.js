/**
 * 设计器拖拽封装 —— 基于 vue-draggable-plus 的 useDraggable
 * vue-draggable-plus 内部自动处理 SortableJS 与 Vue 响应式数组的同步，
 * 我们只负责：左侧组件库 clone、拖入时把 metadata 转成实际组件配置、高亮。
 */
(function () {
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
        var dragName = getDragName(item, scope);
        var targetNode = getContainerNode(wrap, scope);
        if (!dragName || !targetNode || !window.DynDesignerOps) return false;
        if (!window.DynDesignerOps.checkCanDrop(targetNode.component, dragName)) return false;
        var dragUid = item.getAttribute && item.getAttribute("data-dyn-uid");
        if (dragUid) {
            if (targetNode.__uid === dragUid) return false;
            if (window.DynDesignerOps.isDescendantOf(targetNode, window.DynDesignerOps.findNode(scope.pageJson, dragUid))) return false;
        }
        return true;
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

            onMove: function (evt) {
                // 清除上一个目标的红绿标记
                clearDropMarks();
                // evt.to = 鼠标当前悬停的列表根元素（.dyn-children-wrap）
                var target = evt.to;
                var wrap = target && target.classList && target.classList.contains("dyn-children-wrap")
                    ? target
                    : (target && target.closest ? target.closest(".dyn-children-wrap") : null);
                if (!wrap) return false;
                var ok = canAccept(wrap, evt.dragged || evt.item, scope);
                wrap.classList.add(ok ? "dyn-container-hover" : "dyn-container-denied");
                // 返回 false 直接禁止放入（SortableJS 会回弹并显示禁止光标）
                return ok;
            },

            onEnd: function () {
                setDragActive(false);
                listEl.classList.remove("dyn-dragging-active");
                clearDropMarks();
                if (scope) scope.dragging = null;
                if (scope && window.DynDesignerOps) {
                    setTimeout(function () { window.DynDesignerOps.updateOverlay(scope); }, 50);
                }
            },

            onUnchoose: function () {
                setDragActive(false);
                listEl.classList.remove("dyn-dragging-active");
                clearDropMarks();
                if (scope) scope.dragging = null;
            },

            // 从左侧拖入：vue-draggable-plus 已经把 metadata 对象 splice 到 childrenctrls
            // 我们在这里把它替换成真正的组件配置
            onAdd: function (evt) {
                try {
                    var newIndex = evt.newIndex;
                    if (newIndex == null) newIndex = parentCfg.childrenctrls.length - 1;

                    // vue-draggable-plus 自动插入的是左侧 metaList 中的 metadata 对象
                    var inserted = parentCfg.childrenctrls[newIndex];
                    if (!inserted) return;

                    var componentName = inserted.ComponentName || inserted.componentName ||
                        (evt.item && evt.item.getAttribute && evt.item.getAttribute("data-meta"));
                    if (!componentName) return;

                    var meta = findMeta(componentName, scope);
                    var defaults = meta && (meta.DefaultConfigJson || meta.defaultConfigJson);

                    var newCfg;
                    if (typeof defaults === "string") newCfg = JSON.parse(defaults);
                    else if (defaults && typeof defaults === "object") newCfg = JSON.parse(JSON.stringify(defaults));
                    else newCfg = { component: componentName, modelname: "", options: { comoptions:{}, labeloptions:{ label:"", show:true }, itemoptions:{ style:{}, class:"" } }, childrenctrls: [], validators: [], slots: {}, extendinfo: {} };

                    newCfg.__uid = genUid();
                    if (!newCfg.component) newCfg.component = componentName;
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
                    console.error("[useDraggable onAdd]", e);
                }
            }
        });
    };

    DesignerSortable.destroy = function (ins) {
        if (ins && typeof ins.destroy === "function") { try { ins.destroy(); } catch (e) {} }
    };

    window.DesignerSortable = DesignerSortable;
})();

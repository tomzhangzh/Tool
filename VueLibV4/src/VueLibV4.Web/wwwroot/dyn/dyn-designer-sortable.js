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
            preventOnFilter: true
        });
    };

    /**
     * 画布容器：接收 clone + 内部排序，useDraggable 自动同步 childrenctrls 数组
     */
    DesignerSortable.createCanvasContainer = function (listEl, parentCfg, scope, isGrid) {
        var useDraggable = getUseDraggable();
        if (!listEl || !useDraggable || !parentCfg) return null;
        if (!Array.isArray(parentCfg.childrenctrls)) parentCfg.childrenctrls = [];

        var highlightTarget = null;
        function clearHighlight() {
            if (highlightTarget) {
                highlightTarget.classList.remove("dyn-container-hover");
                highlightTarget = null;
            }
        }

        return useDraggable(listEl, parentCfg.childrenctrls, {
            group: { name: "dyn-designer", pull: false, put: true },
            sort: true,
            draggable: "[data-dyn-uid]",
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

            onStart: function () {
                listEl.classList.add("dyn-dragging-active");
            },

            onMove: function (evt) {
                // 清除所有高亮
                document.querySelectorAll(".dyn-container-hover").forEach(function (el) {
                    el.classList.remove("dyn-container-hover");
                });
                // 找鼠标下最内层的 dyn-children-wrap 高亮
                var target = evt.to;
                if (target) {
                    var wrap = target.classList && target.classList.contains("dyn-children-wrap")
                        ? target
                        : (target.closest ? target.closest(".dyn-children-wrap") : null);
                    if (wrap) {
                        wrap.classList.add("dyn-container-hover");
                        highlightTarget = wrap;
                    }
                }
                return true;
            },

            onEnd: function () {
                listEl.classList.remove("dyn-dragging-active");
                document.querySelectorAll(".dyn-container-hover").forEach(function (el) {
                    el.classList.remove("dyn-container-hover");
                });
                if (scope && window.DynDesignerOps) {
                    setTimeout(function () { window.DynDesignerOps.updateOverlay(scope); }, 50);
                }
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

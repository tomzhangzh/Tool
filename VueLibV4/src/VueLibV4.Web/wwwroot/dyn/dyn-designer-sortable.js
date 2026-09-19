/**
 * 设计器 SortableJS 拖拽封装
 * 依赖 SortableJS (Sortable 全局变量由 dyn-lib 加载)
 * 左侧组件库 clone 拖拽 + 画布容器内部排序
 */
(function () {
    var DesignerSortable = {};

    function genUid() {
        return "dyn-" + Math.random().toString(36).slice(2, 10) + Date.now().toString(36).slice(-4);
    }

    /**
     * 左侧组件库：clone 模式，源卡片保留
     */
    DesignerSortable.createComponentLibrary = function (el, scope) {
        if (!el || !window.Sortable) return null;
        return new Sortable(el, {
            group: { name: "dyn-designer", pull: "clone", put: false },
            sort: false,
            animation: 150,
            ghostClass: "dyn-sortable-ghost",
            dragClass: "dyn-sortable-drag"
        });
    };

    /**
     * 画布容器：接收 clone + 内部排序
     */
    DesignerSortable.createCanvasContainer = function (listEl, parentCfg, scope, isGrid) {
        if (!listEl || !window.Sortable || !parentCfg) return null;
        if (!Array.isArray(parentCfg.childrenctrls)) parentCfg.childrenctrls = [];

        return new Sortable(listEl, {
            group: "dyn-designer",
            animation: 180,
            ghostClass: "dyn-designer-ghost",
            chosenClass: "dyn-designer-chosen",
            dragClass: "dyn-designer-dragging",
            fallbackOnBody: true,
            swapThreshold: 0.65,
            onMove: function () { return true; },

            onAdd: function (evt) {
                try {
                    // 移除 sortable 自动插入的临时 DOM
                    var draggedEl = evt.item;
                    draggedEl.remove();

                    var componentName = draggedEl.getAttribute("data-meta") ||
                        (evt.clone && evt.clone.getAttribute("data-meta"));
                    if (!componentName) {
                        console.warn("[Sortable] onAdd: no data-meta on dragged element");
                        return;
                    }

                    var toIndex = (evt.newIndex != null) ? evt.newIndex : parentCfg.childrenctrls.length;
                    if (toIndex > parentCfg.childrenctrls.length) toIndex = parentCfg.childrenctrls.length;

                    // 从共享 scope 的 componentMetaList 找 DefaultConfigJson
                    var defaults = null;
                    if (scope && scope.componentMetaList) {
                        for (var i = 0; i < scope.componentMetaList.length; i++) {
                            var meta = scope.componentMetaList[i];
                            var name = meta.ComponentName || meta.componentName;
                            if (name === componentName) {
                                defaults = meta.DefaultConfigJson || meta.defaultConfigJson;
                                break;
                            }
                        }
                    }

                    var newCfg;
                    if (typeof defaults === "string") {
                        newCfg = JSON.parse(defaults);
                    } else if (defaults && typeof defaults === "object") {
                        newCfg = JSON.parse(JSON.stringify(defaults));
                    } else {
                        // fallback: 异步从 API 获取
                        newCfg = { component: componentName, modelname: "", options: { comoptions:{}, labeloptions:{ label:"", show:true }, itemoptions:{ style:{}, class:"" } }, childrenctrls: [], validators: [], slots: {}, extendinfo: {} };
                        console.warn("[Sortable] no DefaultConfigJson for", componentName, "using bare fallback");
                    }
                    newCfg.uid = genUid();
                    if (!newCfg.component) newCfg.component = componentName;
                    if (!newCfg.options) newCfg.options = {};

                    parentCfg.childrenctrls.splice(toIndex, 0, newCfg);

                    if (isGrid) {
                        if (!newCfg.options.itemoptions) newCfg.options.itemoptions = { style: {}, class: "" };
                        var cls = newCfg.options.itemoptions.class || "";
                        if (cls.indexOf("grid-span-2") < 0) {
                            newCfg.options.itemoptions.class = (cls + " grid-span-2").trim();
                        }
                    }

                    if (scope && window.DynDesignerOps) window.DynDesignerOps.select(scope, newCfg);
                } catch (e) {
                    console.error("[Sortable] onAdd error", e);
                }
            },

            onUpdate: function (evt) {
                try {
                    evt.item.remove();
                    var oldIdx = evt.oldIndex;
                    var newIdx = evt.newIndex;
                    if (oldIdx === newIdx || oldIdx == null || newIdx == null) return;
                    var item = parentCfg.childrenctrls.splice(oldIdx, 1)[0];
                    parentCfg.childrenctrls.splice(newIdx, 0, item);
                    if (scope && window.DynDesignerOps) window.DynDesignerOps.select(scope, item);
                } catch (e) {
                    console.error("[Sortable] onUpdate error", e);
                }
            },

            onEnd: function () {
                if (scope && window.DynDesignerOps) {
                    setTimeout(function () { window.DynDesignerOps.updateOverlay(scope); }, 50);
                }
            }
        });
    };

    DesignerSortable.destroy = function (ins) {
        if (ins && ins.destroy) { try { ins.destroy(); } catch (e) {} }
    };

    window.DesignerSortable = DesignerSortable;
})();

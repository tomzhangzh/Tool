/* ============================================================================
 * designer.core.js —— 低代码设计器内核共享层（多 app 拆分的基石）
 * 各面板（toolbar / palette / canvas / property / dialogs）各自成为独立 Vue app
 * （dyn-init），通过本文件的全局共享 store 读写同一份页面数据，
 * 通过 dyn.eventBus 广播状态变化事件，通过 __lcApi 调用内核方法。
 * ============================================================================ */
(function (global) {
    'use strict';
    if (!global.Vue) { console.error('[designer.core] 请先引入 Vue3（vue.global.js）'); return; }
    var Vue = global.Vue;

    // ===== 全局共享状态（ref 风格与主 app 保持一致，模板绑定方式不变）=====
    var componentMetaList = Vue.ref([]);   // 组件元数据（左侧组件库）
    var pageList = Vue.ref([]);            // 页面列表
    var currentPageCode = Vue.ref('');     // 当前页面编码
    var currentPageId = Vue.ref(null);     // 当前页面 Id
    var saving = Vue.ref(false);           // 保存中
    var designMode = Vue.ref('design');    // design | preview
    var canvasPlatform = Vue.ref('desktop');// mobile | desktop
    var canvasZoom = Vue.ref(1);           // 画布缩放
    var showRuler = Vue.ref(true);         // 标尺显示
    var showJson = Vue.ref(false);         // JSON 弹窗
    var canvasWidth = Vue.ref(1200);       // 桌面画布宽
    var canvasHeight = Vue.ref(800);       // 桌面画布高
    var currentCom = Vue.ref(null);        // 当前选中组件
    var currentContainer = Vue.ref(null);  // 选中组件父容器
    var currentPath = Vue.ref('');         // 选中组件路径
    var breadcrumbList = Vue.ref([]);      // 面包屑
    var treeVersion = Vue.ref(0);          // 组件树刷新版本
    var openConfigMode = Vue.ref(false);   // 开放配置开关（顶部工具栏）

    // 页面配置根节点（画布/属性面板/JSON/组件树共用同一 reactive 对象）
    var configObj = Vue.reactive({
        component: 'DynNDivContainer', modelname: '',
        options: {
            comoptions: {}, comlisteners: {}, labeloptions: {},
            itemoptions: { style: { padding: '12px', background: '#fff' }, class: '' }
        },
        validators: [], childrenctrls: [], slots: {}, extendinfo: {}
    });
    var modelObj = Vue.reactive({});

    // ===== 弹窗（dialogs 独立 app）共享 UI 状态 =====
    var showNewPage = Vue.ref(false);
    var showModelModal = Vue.ref(false);
    var showCompositeDialog = Vue.ref(false);
    var configJsonText = Vue.ref('');
    var newPageForm = Vue.reactive({ pageName: '', pageCode: '' });
    var compositeForm = Vue.reactive({
        componentName: '', label: '', icon: '📦', source: 'selected', description: '',
        exposedProps: [], openContainers: []
    });
    var modelJsonText = Vue.computed(function () { return JSON.stringify(modelObj, null, 2); });

    var zoomPercent = Vue.computed(function () { return Math.round(canvasZoom.value * 100) + '%'; });

    global.LCDesignerStore = {
        componentMetaList: componentMetaList,
        pageList: pageList,
        currentPageCode: currentPageCode,
        currentPageId: currentPageId,
        saving: saving,
        designMode: designMode,
        canvasPlatform: canvasPlatform,
        canvasZoom: canvasZoom,
        showRuler: showRuler,
        showJson: showJson,
        canvasWidth: canvasWidth,
        canvasHeight: canvasHeight,
        currentCom: currentCom,
        currentContainer: currentContainer,
        currentPath: currentPath,
        breadcrumbList: breadcrumbList,
        treeVersion: treeVersion,
        openConfigMode: openConfigMode,
        configObj: configObj,
        modelObj: modelObj,
        showNewPage: showNewPage,
        showModelModal: showModelModal,
        showCompositeDialog: showCompositeDialog,
        configJsonText: configJsonText,
        newPageForm: newPageForm,
        compositeForm: compositeForm,
        modelJsonText: modelJsonText,
        zoomPercent: zoomPercent
    };

    // ===== 公共 API 容器：主 app（内核）在 setup 中把方法挂进来，各面板 app 调用 =====
    global.__lcApi = global.__lcApi || { VERSION: '1.0.0' };

})(window);

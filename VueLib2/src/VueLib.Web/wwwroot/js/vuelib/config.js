/**
 * ============================================================================
 * VueLib2 全局配置（所有可调常量集中在此文件头部，便于统一修改）
 * ============================================================================
 */
(function () {
    'use strict';

    window.VueLibConfig = {
        /* ---------- 调试开关 ---------- */
        debug: true,                       // window.__DYN_DEBUG 由此处控制；true 输出 [DynChain] 结构化日志
        debugAttr: 'dyn-debug',            // DOM 调试属性：带此属性打印该元素解析详情

        /* ---------- 组件加载 ---------- */
        componentLoadMode: 'lazy',         // 'lazy'：请求组件时到服务器端加载；'all'：启动时一次全部加载
        componentApi: '/api/lowcode/component/render',   // 组件渲染快照接口（name=&version=）
        componentMetaApi: '/api/lowcode/components',     // 组件元数据接口
        componentVersionLock: true,        // 全局开关：页面锁定组件版本（页面配置 extendinfo.componentVersions 优先）
        componentCacheSize: 100,           // 组件缓存 LRU 上限
        asyncDelay: 200,                   // defineAsyncComponent delay
        asyncTimeout: 15000,               // 组件加载超时(ms)

        /* ---------- 页面 ---------- */
        pageApi: '/api/lowcode/page',      // 页面详情/保存接口
        pagesApi: '/api/lowcode/pages',    // 页面列表接口

        /* ---------- 下拉选项 ---------- */
        optionsResolveApi: '/api/lowcode/options/resolve',

        /* ---------- 默认 UI ---------- */
        uiLibrary: 'element-plus',         // 当前 UI 库（NutUI 后续接入）
        defaultRedirect: '/#/pages',

        /* ---------- 窗口 ---------- */
        windowDefaults: { width: '600px', height: '420px' },

        /* ---------- 渲染 ---------- */
        designDepthLimit: 15               // 组件树递归深度上限（防循环 JSON 栈溢出）
    };

    // 全局开关（旧代码兼容读取）
    window.__DYN_DEBUG = window.VueLibConfig.debug;
})();

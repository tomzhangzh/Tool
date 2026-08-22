/**
 * vue-loader.js
 * 核心模块：vueLoadCom - 将数据库中的组件定义动态加载为 Vue 3 异步组件
 *
 * 工作原理：
 *   1. vueLoadCom(name) 返回 Vue 3 defineAsyncComponent 包装的异步组件
 *   2. 组件首次渲染时，从 GET /api/component/define/{name} 拉取完整定义
 *   3. 解析 scriptContent (export default {...}) → 组件选项对象
 *   4. 注入 templateContent 到选项的 template 字段
 *   5. 动态注入 styleContent 到 <head>
 *   6. 缓存已加载组件，避免重复请求
 */
(function (global) {
    'use strict';

    // 已加载组件缓存: name -> component options
    const componentCache = new Map();

    // 正在加载中的 Promise: name -> Promise
    const loadingPromises = new Map();

    // 已注入的 style 标记集合
    const injectedStyles = new Set();

    // API 基础路径（可被外部覆盖）
    let API_BASE = '/api/component';

    /**
     * 配置 API 基础路径
     */
    function config(options) {
        if (options && options.apiBase) {
            API_BASE = options.apiBase.replace(/\/$/, '');
        }
    }

    /**
     * 从 API 获取组件完整定义
     */
    async function fetchComponentDefine(componentName) {
        const url = `${API_BASE}/define/${encodeURIComponent(componentName)}`;
        const resp = await fetch(url, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });
        if (!resp.ok) {
            throw new Error(`加载组件 [${componentName}] 失败: HTTP ${resp.status}`);
        }
        const result = await resp.json();
        if (!result.success || !result.data) {
            throw new Error(`加载组件 [${componentName}] 失败: ${result.message || '未知错误'}`);
        }
        return result.data;
    }

    /**
     * 解析 scriptContent 为组件选项对象
     * 数据库中存储格式: export default { name, data, methods, ... }
     * 在 UMD 环境下通过 new Function() 执行并返回对象
     */
    function parseScriptContent(scriptContent, componentName) {
        if (!scriptContent || !scriptContent.trim()) {
            return { name: componentName };
        }

        // 去掉 "export default" 前缀，转为 return 语句
        let code = scriptContent.trim();
        code = code.replace(/^\s*export\s+default\s*/, 'return ');

        // 如果没有 return 前缀（兼容纯对象写法），加上 return
        if (!/^\s*return\s+/.test(code) && code.startsWith('{')) {
            code = 'return ' + code;
        }

        try {
            const factory = new Function(code);
            const options = factory();
            if (!options || typeof options !== 'object') {
                throw new Error('script 必须导出一个对象');
            }
            // 确保 name 存在
            if (!options.name) {
                options.name = componentName;
            }
            return options;
        } catch (err) {
            console.error(`[vueLoadCom] 解析组件 [${componentName}] script 失败:`, err);
            console.error('Script 内容:', scriptContent);
            throw new Error(`组件 [${componentName}] script 解析失败: ${err.message}`);
        }
    }

    /**
     * 动态注入组件样式到 <head>
     */
    function injectStyle(componentName, styleContent) {
        if (!styleContent || !styleContent.trim()) return;
        if (injectedStyles.has(componentName)) return;

        const styleEl = document.createElement('style');
        styleEl.setAttribute('data-component', componentName);
        styleEl.textContent = styleContent;
        document.head.appendChild(styleEl);
        injectedStyles.add(componentName);
    }

    /**
     * 将组件定义转换为 Vue 组件选项对象
     */
    async function loadComponent(componentName) {
        // 命中缓存
        if (componentCache.has(componentName)) {
            return componentCache.get(componentName);
        }

        // 避免并发重复请求
        if (loadingPromises.has(componentName)) {
            return loadingPromises.get(componentName);
        }

        const loadPromise = (async () => {
            try {
                const define = await fetchComponentDefine(componentName);

                // 解析 script → 组件选项
                const options = parseScriptContent(define.scriptContent, componentName);

                // 注入 template
                options.template = define.templateContent;

                // 注入样式
                injectStyle(componentName, define.styleContent);

                // 缓存
                componentCache.set(componentName, options);
                console.log(`[vueLoadCom] 组件 [${componentName}] 加载成功`);
                return options;
            } catch (err) {
                // 加载失败时清除 loading 状态，允许重试
                loadingPromises.delete(componentName);
                throw err;
            }
        })();

        loadingPromises.set(componentName, loadPromise);
        return loadPromise;
    }

    /**
     * 核心函数：vueLoadCom
     * 返回一个 Vue 3 异步组件定义（可直接用于 app.component 或路由 component）
     *
     * @param {string} componentName - 组件名称（对应数据库 ComponentName）
     * @param {object} [asyncOptions] - defineAsyncComponent 的额外选项 (delay, timeout, loadingComponent, errorComponent)
     * @returns {object} Vue 3 异步组件定义
     */
    function vueLoadCom(componentName, asyncOptions) {
        if (!global.Vue || !global.Vue.defineAsyncComponent) {
            throw new Error('Vue 3 未加载，请先引入 vue.global.prod.js');
        }

        const loader = () => loadComponent(componentName);

        // 默认的加载中 / 错误组件
        const defaultOptions = {
            delay: 200,
            timeout: 10000,
            loadingComponent: {
                template: `<div style="padding:20px;text-align:center;color:#909399;">
                    <span style="display:inline-block;width:20px;height:20px;border:2px solid #dcdfe6;border-top-color:#409eff;border-radius:50%;animation:spin 0.8s linear infinite;"></span>
                    <span style="margin-left:8px;">组件加载中...</span>
                    <style>@keyframes spin{to{transform:rotate(360deg)}}</style>
                </div>`
            },
            errorComponent: {
                props: ['error'],
                template: `<div style="padding:20px;text-align:center;color:#f56c6c;border:1px dashed #f56c6c;border-radius:4px;margin:8px;">
                    <strong>组件加载失败</strong><br/>
                    <span style="font-size:12px;">{{ error && error.message }}</span>
                </div>`
            }
        };

        return global.Vue.defineAsyncComponent(
            Object.assign({}, defaultOptions, asyncOptions || {}, { loader })
        );
    }

    /**
     * 批量预加载组件（不阻塞，后台加载）
     */
    function preload(componentNames) {
        if (!Array.isArray(componentNames)) return;
        componentNames.forEach(name => {
            if (!componentCache.has(name)) {
                loadComponent(name).catch(err => {
                    console.warn(`[vueLoadCom] 预加载组件 [${name}] 失败:`, err.message);
                });
            }
        });
    }

    /**
     * 清除组件缓存（用于热更新场景）
     */
    function clearCache(componentName) {
        if (componentName) {
            componentCache.delete(componentName);
            injectedStyles.delete(componentName);
        } else {
            componentCache.clear();
            injectedStyles.clear();
        }
    }

    // 暴露到全局
    global.vueLoadCom = vueLoadCom;
    global.vueLoadCom.config = config;
    global.vueLoadCom.preload = preload;
    global.vueLoadCom.clearCache = clearCache;
    global.vueLoadCom.getCache = () => componentCache;

})(window);

/**
 * nut-runtime.js
 * NutUI 低代码平台运行时
 * 功能：
 *   1. nutLoadCom - 从 Razor View 动态加载组件（解析 template + comconfig script）
 *   2. nutInit    - 初始化 NutUI 应用（注册组件、构建路由）
 *   3. nutRender  - 渲染页面配置树
 *   4. nutValidate - 表单验证器系统
 */
(function (global) {
    'use strict';

    // 暴露 Vue 全局 API，供动态组件 setup 函数使用
    const _vueGlobals = ['computed', 'reactive', 'ref', 'watch', 'onMounted', 'onUnmounted', 'nextTick', 'defineAsyncComponent', 'markRaw', 'h', 'provide', 'inject'];
    _vueGlobals.forEach(name => { if (Vue[name] && !global[name]) global[name] = Vue[name]; });

    const { createApp, defineAsyncComponent, reactive, ref, computed, h } = Vue;

    // ========== 组件缓存 ==========
    const componentCache = new Map();
    const loadingPromises = new Map();
    // 自定义脚本缓存（组件名 -> 自定义脚本配置）
    const customScriptCache = new Map();

    /**
     * 注册组件的自定义脚本
     * @param {string} componentName - 组件名
     * @param {object|string} customScript - 自定义脚本配置 { methods, onMounted, watch }
     */
    function registerCustomScript(componentName, customScript) {
        if (!customScript) return;
        const config = typeof customScript === 'string' ? JSON.parse(customScript) : customScript;
        customScriptCache.set(componentName, config);
    }

    /**
     * 将自定义脚本合并到组件配置中
     */
    function applyCustomScript(comConfig, componentName) {
        const custom = customScriptCache.get(componentName);
        if (!custom) return comConfig;

        const originalSetup = comConfig.setup;
        if (!originalSetup) return comConfig;

        comConfig.setup = function (props, context) {
            const result = originalSetup(props, context) || {};
            const { onMounted, watch } = Vue;

            // 合并自定义 methods
            if (custom.methods) {
                for (const name in custom.methods) {
                    const fnBody = custom.methods[name];
                    if (typeof fnBody === 'string') {
                        try {
                            result[name] = new Function('props', 'modelinfo', 'comInnerInfo', 'context', fnBody).bind(result, props, result.modelinfo, result.comInnerInfo, context);
                        } catch (e) {
                            console.error(`[CustomScript] 解析方法 ${name} 失败:`, e);
                        }
                    } else if (typeof fnBody === 'function') {
                        result[name] = fnBody;
                    }
                }
            }

            // 自定义 onMounted
            if (custom.onMounted && typeof custom.onMounted === 'string') {
                onMounted(() => {
                    try {
                        new Function('props', 'modelinfo', 'comInnerInfo', custom.onMounted).call(result, props, result.modelinfo, result.comInnerInfo);
                    } catch (e) {
                        console.error('[CustomScript] onMounted 执行失败:', e);
                    }
                });
            }

            // 自定义 watch
            if (custom.watch) {
                for (const target in custom.watch) {
                    const handler = custom.watch[target];
                    if (typeof handler === 'string') {
                        watch(() => {
                            // 支持 modelinfo 或 props.xxx
                            if (target === 'modelinfo') return result.modelinfo?.value;
                            return Vue.getByPath ? Vue.getByPath(props, target) : undefined;
                        }, (val) => {
                            try {
                                new Function('val', 'props', 'modelinfo', handler).call(result, val, props, result.modelinfo);
                            } catch (e) {
                                console.error(`[CustomScript] watch ${target} 执行失败:`, e);
                            }
                        });
                    }
                }
            }

            return result;
        };

        return comConfig;
    }

    /**
     * 从 Razor View 加载组件定义
     * Razor View 输出格式:
     *   <template>...</template>
     *   <script tag='comconfig'>var comConfig = {...}</script>
     */
    async function fetchComponentFromRazor(url) {
        const resp = await fetch(url, {
            method: 'GET',
            headers: { 'Accept': 'text/html' }
        });
        if (!resp.ok) throw new Error(`加载组件失败: HTTP ${resp.status} (${url})`);
        const html = await resp.text();

        // 用 DOMParser 解析
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        // 提取 template
        const tplEl = doc.querySelector('template');
        if (!tplEl) throw new Error(`组件 ${url} 缺少 <template>`);
        const template = tplEl.innerHTML;

        // 提取 comconfig script
        const scriptEl = doc.querySelector("script[tag='comconfig']");
        if (!scriptEl) throw new Error(`组件 ${url} 缺少 comconfig script`);
        const scriptText = scriptEl.textContent;

        // 执行 script 获取 comConfig
        const factory = new Function(`${scriptText}; return typeof comConfig !== 'undefined' ? comConfig : null;`);
        const comConfig = factory();
        if (!comConfig) throw new Error(`组件 ${url} 的 comConfig 为空`);

        // 注入 template
        comConfig.template = template;
        return comConfig;
    }

    /**
     * nutLoadCom - 加载并缓存组件
     * @param {string} componentName - 组件注册名
     * @param {string} url - Razor View 地址
     * @returns {object} Vue 异步组件定义
     */
    function nutLoadCom(componentName, url) {
        const loader = async () => {
            if (componentCache.has(componentName)) {
                return componentCache.get(componentName);
            }
            if (loadingPromises.has(componentName)) {
                return loadingPromises.get(componentName);
            }

            const promise = (async () => {
                try {
                    let comp = await fetchComponentFromRazor(url);
                    // 应用自定义脚本
                    comp = applyCustomScript(comp, componentName);
                    componentCache.set(componentName, comp);
                    console.log(`[nutLoadCom] 组件加载成功: ${componentName}`);
                    return comp;
                } catch (err) {
                    loadingPromises.delete(componentName);
                    console.error(`[nutLoadCom] 组件加载失败: ${componentName}`, err);
                    throw err;
                }
            })();
            loadingPromises.set(componentName, promise);
            return promise;
        };

        return defineAsyncComponent({
            loader,
            delay: 200,
            timeout: 15000,
            loadingComponent: {
                template: `<div style="padding:16px;text-align:center;color:#909399;">
                    <span style="display:inline-block;width:16px;height:16px;border:2px solid #dcdfe6;border-top-color:#ee0a24;border-radius:50%;animation:nut-spin .8s linear infinite;"></span>
                    <span style="margin-left:6px;font-size:12px;">加载中...</span>
                    <style>@keyframes nut-spin{to{transform:rotate(360deg)}}</style>
                </div>`
            },
            errorComponent: {
                props: ['error'],
                template: `<div style="padding:12px;color:#ee0a24;font-size:12px;border:1px dashed #ee0a24;border-radius:4px;margin:4px;">
                    组件加载失败: {{ error && error.message }}
                </div>`
            }
        });
    }

    // ========== 验证器系统 ==========
    const builtinValidators = {
        required: (v) => v !== null && v !== undefined && v !== '' && !(Array.isArray(v) && v.length === 0),
        requiredTrue: (v) => v === true,
        minLength: (v, m) => typeof v === 'string' && v.length >= m,
        maxLength: (v, m) => typeof v === 'string' && v.length <= m,
        min: (v, m) => Number(v) >= m,
        max: (v, m) => Number(v) <= m,
        pattern: (v, p) => new RegExp(p).test(String(v || '')),
        email: (v) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v || '')),
        phone: (v) => /^1[3-9]\d{9}$/.test(String(v || '')),
        url: (v) => /^https?:\/\/.+/.test(String(v || '')),
        number: (v) => v !== '' && !isNaN(Number(v))
    };

    /**
     * 验证单个字段
     */
    function validateField(value, rules) {
        const errors = [];
        for (const rule of rules || []) {
            const fn = builtinValidators[rule.type];
            if (!fn) continue;
            if (!fn(value, rule.value)) {
                errors.push(rule.message || `验证失败: ${rule.type}`);
            }
        }
        return { valid: errors.length === 0, errors };
    }

    /**
     * 递归验证整个表单配置
     * @param {object} config - 组件配置树
     * @param {object} model - 数据模型
     * @returns {object} { valid, errors: { field: [messages] } }
     */
    function validateConfig(config, model) {
        const allErrors = {};
        let allValid = true;

        function walk(node) {
            if (!node) return;
            // 验证当前节点
            if (node.modelname && node.validators && node.validators.length > 0) {
                const val = getByPath(model, node.modelname);
                const result = validateField(val, node.validators);
                if (!result.valid) {
                    allErrors[node.modelname] = result.errors;
                    allValid = false;
                }
            }
            // 递归子组件
            if (node.childrenctrls && Array.isArray(node.childrenctrls)) {
                node.childrenctrls.forEach(walk);
            }
            // 递归 slots
            if (node.slots) {
                Object.values(node.slots).forEach(slot => {
                    if (slot && slot.childrenctrls) walk(slot);
                });
            }
        }

        walk(config);
        return { valid: allValid, errors: allErrors };
    }

    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce((o, k) => (o == null ? undefined : o[k]), obj);
    }

    // ========== 应用初始化 ==========

    /**
     * 从 API 获取组件元数据列表
     */
    async function fetchComponentMeta() {
        const resp = await fetch('/api/lowcode/components');
        if (!resp.ok) throw new Error('获取组件元数据失败');
        const result = await resp.json();
        return result.success ? result.data : [];
    }

    /**
     * 初始化 NutUI 低代码应用
     * @param {object} options - { el, onReady }
     */
    async function nutInit(options) {
        const el = typeof options.el === 'string' ? document.querySelector(options.el) : options.el;
        if (!el) throw new Error('nutInit: 未找到挂载元素');

        // 创建 Vue 应用
        const app = createApp({
            template: '<div id="nut-app-root"><router-view></router-view></div>'
        });

        // 注册 NutUI
        if (global.nutui) {
            app.use(global.nutui.default || global.nutui);
        }

        // 注册动态组件渲染器（内置，非异步）
        app.component('NDynamicCom', {
            props: {
                jsonconfig: { type: Object, required: true },
                parentmodelinfo: { type: Object, default: () => ({}) }
            },
            template: `<component :is="jsonconfig.component"
                         :jsonconfig="jsonconfig"
                         :parentmodelinfo="parentmodelinfo"></component>`
        });

        // 获取组件元数据并注册
        const metaList = await fetchComponentMeta();
        for (const meta of metaList) {
            app.component(meta.ComponentName, nutLoadCom(meta.ComponentName, meta.LoadUrl));
        }

        // 构建路由
        const { createRouter, createWebHashHistory } = VueRouter;
        const router = createRouter({
            history: createWebHashHistory(),
            routes: [
                {
                    path: '/page/:code',
                    component: {
                        template: '<div ref="pageContainer"></div>',
                        async mounted() {
                            const code = this.$route.params.code;
                            await this.$nutRenderPageByCode(code, this.$refs.pageContainer, this.$root);
                        }
                    }
                },
                { path: '/', redirect: '/page/user-register' }
            ]
        });

        // 全局方法
        app.config.globalProperties.$nutValidate = validateConfig;
        app.config.globalProperties.$nutRenderPageByCode = async function (code, container, appInstance) {
            const resp = await fetch(`/api/lowcode/page/${code}`);
            const result = await resp.json();
            if (!result.success) throw new Error(result.message);
            const page = result.data;
            const config = JSON.parse(page.ConfigJson);
            const model = JSON.parse(page.DefaultModelJson || '{}');
            renderPageInto(appInstance, container, config, model);
        };

        app.use(router);
        app.mount(el);

        if (typeof options.onReady === 'function') {
            options.onReady(app, metaList);
        }

        return { app, metaList };
    }

    /**
     * 将页面配置渲染到指定容器
     */
    function renderPageInto(parentApp, container, config, model) {
        const reactiveModel = reactive(model || {});

        // 创建一个子应用来渲染页面
        const pageApp = createApp({
            data() {
                return { model: reactiveModel, config };
            },
            template: `<n-dynamic-com :jsonconfig="config" :parentmodelinfo="model"></n-dynamic-com>`,
            methods: {
                validate() {
                    return validateConfig(this.config, this.model);
                },
                getModel() {
                    return this.model;
                }
            }
        });

        // 注册动态渲染器和所有已缓存组件
        pageApp.component('NDynamicCom', {
            props: {
                jsonconfig: { type: Object, required: true },
                parentmodelinfo: { type: Object, default: () => ({}) }
            },
            template: `<component :is="jsonconfig.component"
                         :jsonconfig="jsonconfig"
                         :parentmodelinfo="parentmodelinfo"></component>`
        });

        if (global.nutui) {
            pageApp.use(global.nutui.default || global.nutui);
        }

        pageApp.mount(container);
        return pageApp;
    }

    // ========== 暴露 API ==========
    global.nutLoadCom = nutLoadCom;
    global.nutInit = nutInit;
    global.nutValidate = validateConfig;
    global.nutValidateField = validateField;
    global.nutRenderPage = renderPageInto;
    global.nutBuiltinValidators = builtinValidators;
    global.nutComponentCache = componentCache;
    global.nutRegisterCustomScript = registerCustomScript;

})(window);

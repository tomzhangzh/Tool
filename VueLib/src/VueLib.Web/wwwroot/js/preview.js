/**
 * preview.js - 手机预览页面逻辑
 * 两种模式：
 *   1. iframe 嵌入模式：通过 postMessage 接收设计器配置
 *   2. 独立访问模式：通过 URL 参数 ?code=xxx 从 API 加载页面配置
 */
(function () {
    'use strict';

    // 暴露 Vue 全局 API，供动态组件 setup 函数使用（computed/ref/watch 等）
    const _vueGlobals = ['computed', 'reactive', 'ref', 'watch', 'onMounted', 'onUnmounted', 'nextTick', 'defineAsyncComponent', 'markRaw', 'h', 'provide', 'inject'];
    _vueGlobals.forEach(name => { if (Vue[name] && !window[name]) window[name] = Vue[name]; });

    const { createApp, reactive } = Vue;

    let appInstance = null;
    let currentModel = null;
    let currentConfig = null;

    // 获取 URL 参数
    function getUrlParam(name) {
        const match = window.location.search.match(new RegExp('[?&]' + name + '=([^&]+)'));
        return match ? decodeURIComponent(match[1]) : null;
    }

    const pageCode = getUrlParam('code');
    const isStandalone = !!pageCode;

    // 注册 NutUI
    function registerNutUI(app) {
        if (window.nutui) {
            const nutuiObj = window.nutui.default || window.nutui;
            app.use(nutuiObj);
            // 保险：遍历 NutUI 导出的所有组件，手动注册 kebab-case 别名
            let manualCount = 0;
            for (const key in nutuiObj) {
                const comp = nutuiObj[key];
                if (comp && (typeof comp === 'object' || typeof comp === 'function') && comp.name && comp.render) {
                    // PascalCase -> kebab-case: NutButton -> nut-button
                    const kebab = comp.name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
                    if (!app._context.components[kebab]) {
                        app.component(kebab, comp);
                        manualCount++;
                    }
                }
            }
            const comps = Object.keys(app._context.components);
            console.log('[Preview] NutUI 组件总数:', comps.length, '(手动注册 kebab 别名:', manualCount, ')');
            console.log('[Preview] 组件名样例:', comps.slice(0, 40).join(', '));
        } else {
            console.error('[Preview] window.nutui 不存在!');
        }
    }

    // 注册动态渲染器
    function registerDynamicCom(app) {
        app.component('NDynamicCom', {
            props: {
                jsonconfig: { type: Object, required: true },
                parentmodelinfo: { type: Object, default: () => ({}) }
            },
            template: `<component :is="jsonconfig.component"
                         :jsonconfig="jsonconfig"
                         :parentmodelinfo="parentmodelinfo"></component>`
        });
    }

    // 渲染配置
    function renderConfig(config, model) {
        console.log('[Preview] renderConfig called, root component:', config?.component, 'children:', config?.childrenctrls?.length);
        const container = document.getElementById('preview-app');
        if (!container) { console.error('[Preview] #preview-app not found'); return; }

        if (appInstance) {
            appInstance.unmount();
            appInstance = null;
        }

        currentConfig = config;
        currentModel = reactive(model || {});

        const app = createApp({
            data() {
                return { config: config || {}, model: currentModel };
            },
            template: `<div class="preview-page">
                <n-dynamic-com :jsonconfig="config" :parentmodelinfo="model"></n-dynamic-com>
                <div v-if="isStandalone" class="standalone-actions">
                    <button class="btn-primary" @@click="handleSubmit">提交</button>
                    <button class="btn-default" @@click="handleReset">重置</button>
                </div>
            </div>`,
            methods: {
                validate() {
                    return window.nutValidate(this.config, this.model);
                },
                getModel() {
                    return this.model;
                },
                handleSubmit() {
                    const result = this.validate();
                    if (result.valid) {
                        alert('验证通过！\n数据:\n' + JSON.stringify(this.model, null, 2));
                    } else {
                        const msgs = Object.values(result.errors).flat().join('\n');
                        alert('验证失败：\n' + msgs);
                    }
                },
                handleReset() {
                    Object.keys(this.model).forEach(k => delete this.model[k]);
                }
            }
        });

        app.config.globalProperties.isStandalone = isStandalone;
        registerNutUI(app);
        registerDynamicCom(app);

        loadAndRegisterComponents(app).then(() => {
            appInstance = app.mount(container);
            if (!isStandalone) {
                try { parent.postMessage({ type: 'preview-ready' }, '*'); } catch (e) {}
            }
        });
    }

    // 从 API 加载组件元数据并注册
    async function loadAndRegisterComponents(app) {
        try {
            const resp = await fetch('/api/lowcode/components');
            const result = await resp.json();
            if (result.success && result.data) {
                let registered = 0;
                for (const meta of result.data) {
                    // 兼容 camelCase 和 PascalCase
                    const name = meta.componentName || meta.ComponentName;
                    const url = meta.loadUrl || meta.LoadUrl;
                    if (name && url) {
                        app.component(name, window.nutLoadCom(name, url));
                        registered++;
                    } else {
                        console.warn('[Preview] 组件元数据缺少 name 或 url:', meta);
                    }
                }
                console.log(`[Preview] 实际注册了 ${registered}/${result.data.length} 个组件`);
                console.log('[Preview] 已注册组件:', Object.keys(app._context.components).filter(k => k.startsWith('N')).join(', '));
            }
        } catch (err) {
            console.error('[Preview] 加载组件元数据失败:', err);
        }
    }

    // 独立模式：从 API 加载页面配置
    async function loadPageByCode(code) {
        try {
            const resp = await fetch(`/api/lowcode/page/${code}`);
            const result = await resp.json();
            if (result.success && result.data) {
                const config = JSON.parse(result.data.configJson || '{}');
                const model = JSON.parse(result.data.defaultModelJson || '{}');
                document.title = result.data.pageName || '页面预览';
                renderConfig(config, model);
            } else {
                showError('加载页面失败: ' + (result.message || '未知错误'));
            }
        } catch (err) {
            showError('加载页面失败: ' + err.message);
        }
    }

    function showError(msg) {
        document.getElementById('preview-app').innerHTML =
            `<div style="padding:40px;text-align:center;color:#ee0a24;">
                <h3>页面加载失败</h3>
                <p>${msg}</p>
            </div>`;
    }

    // 监听设计器消息（iframe 模式）
    window.addEventListener('message', (event) => {
        const data = event.data;
        if (!data || !data.type) return;
        console.log('[Preview] 收到消息:', data.type);

        switch (data.type) {
            case 'designer-update':
                if (data.config) {
                    renderConfig(data.config, data.model || {});
                }
                break;
            case 'designer-validate':
                if (appInstance) {
                    const result = appInstance.validate();
                    try { parent.postMessage({ type: 'validate-result', data: result }, '*'); } catch (e) {}
                }
                break;
            case 'designer-get-model':
                if (currentModel) {
                    try { parent.postMessage({ type: 'model-result', data: JSON.parse(JSON.stringify(currentModel)) }, '*'); } catch (e) {}
                }
                break;
        }
    });

    // 初始化
    if (isStandalone) {
        // 独立模式：加载页面
        loadPageByCode(pageCode);
    } else {
        // iframe 模式：通知设计器已加载
        try { parent.postMessage({ type: 'preview-loaded' }, '*'); } catch (e) {}

        // 超时提示：5秒内未收到配置则显示提示
        setTimeout(() => {
            if (!appInstance) {
                const el = document.getElementById('preview-app');
                if (el && el.querySelector('.preview-loading')) {
                    el.innerHTML = `
                        <div style="padding:30px;text-align:center;color:#909399;font-size:13px;">
                            <div style="font-size:40px;margin-bottom:12px;">📱</div>
                            <p>预览就绪，等待设计器配置...</p>
                            <p style="font-size:11px;color:#c0c4cc;margin-top:8px;">
                                如长时间无内容，请检查设计器页面是否正常加载
                            </p>
                        </div>`;
                }
            }
        }, 5000);
    }

})();

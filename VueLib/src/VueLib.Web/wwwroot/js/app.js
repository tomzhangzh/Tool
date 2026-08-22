/**
 * app.js
 * 应用入口：
 *   1. 从 /api/component/list 获取已启用组件清单
 *   2. 公共组件 → app.component() 全局注册为异步组件
 *   3. 页面组件 → 构建 Vue Router 路由表
 *   4. 创建 Vue app + Router，挂载到 #app
 */
(function () {
    'use strict';

    const { createApp, defineAsyncComponent } = Vue;
    const { createRouter, createWebHashHistory, createWebHistory } = VueRouter;

    // API 基础路径
    const API_BASE = '/api/component';

    /**
     * 从 API 获取组件清单
     */
    async function fetchComponentList() {
        const resp = await fetch(`${API_BASE}/list`, {
            headers: { 'Accept': 'application/json' }
        });
        if (!resp.ok) throw new Error(`获取组件清单失败: HTTP ${resp.status}`);
        const result = await resp.json();
        if (!result.success || !result.data) {
            throw new Error(`获取组件清单失败: ${result.message || '未知错误'}`);
        }
        return result.data;
    }

    /**
     * 构建路由表
     * componentType=2 (Page) 的组件转为路由记录
     */
    function buildRoutes(components) {
        const routes = [];
        const pageComponents = components.filter(c => c.componentType === 2);

        pageComponents.forEach(c => {
            const path = c.routePath || `/${c.componentName.toLowerCase()}`;
            routes.push({
                path: path,
                name: c.componentName,
                component: vueLoadCom(c.componentName),
                meta: { title: c.description || c.componentName }
            });
        });

        // 兜底路由：未匹配时重定向到首页
        const hasRoot = routes.some(r => r.path === '/');
        if (!hasRoot && routes.length > 0) {
            routes.push({ path: '/', redirect: routes[0].path });
        }
        routes.push({ path: '/:pathMatch(.*)*', redirect: '/' });

        return routes;
    }

    /**
     * 全局注册公共组件
     * componentType=1 (Common) 的组件注册为全局异步组件
     */
    function registerGlobalComponents(app, components) {
        const commonComponents = components.filter(c => c.componentType === 1);
        commonComponents.forEach(c => {
            app.component(c.componentName, vueLoadCom(c.componentName));
            console.log(`[App] 全局注册公共组件: ${c.componentName}`);
        });
        return commonComponents.length;
    }

    /**
     * 启动应用
     */
    async function bootstrap() {
        const appElement = document.getElementById('app');
        if (!appElement) {
            console.error('[App] 未找到 #app 挂载点');
            return;
        }

        // 显示加载状态
        appElement.innerHTML = `
            <div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;color:#606266;">
                <div style="width:40px;height:40px;border:3px solid #dcdfe6;border-top-color:#409eff;border-radius:50%;animation:spin 0.8s linear infinite;"></div>
                <p style="margin-top:16px;">正在从数据库加载组件定义...</p>
                <style>@keyframes spin{to{transform:rotate(360deg)}}</style>
            </div>`;

        try {
            // 1. 获取组件清单
            const components = await fetchComponentList();
            console.log(`[App] 获取到 ${components.length} 个组件定义`);

            // 2. 创建 Vue app
            const app = createApp({
                template: `
                    <div id="app-root">
                        <nav class="app-nav">
                            <span class="app-brand">VueLib 动态组件库</span>
                            <router-link to="/" class="nav-link">首页</router-link>
                            <router-link to="/about" class="nav-link">关于</router-link>
                            <router-link to="/users" class="nav-link">用户列表</router-link>
                        </nav>
                        <main class="app-main">
                            <router-view></router-view>
                        </main>
                    </div>
                `
            });

            // 3. 全局注册公共组件
            const commonCount = registerGlobalComponents(app, components);

            // 4. 构建路由
            const routes = buildRoutes(components);
            console.log(`[App] 构建 ${routes.length} 条路由 (${commonCount} 个公共组件)`);

            const router = createRouter({
                history: createWebHashHistory(), // 使用 hash 模式，兼容 IIS/Kestrel 无 URL Rewrite
                routes: routes
            });

            // 路由变更时更新页面标题
            router.afterEach((to) => {
                if (to.meta && to.meta.title) {
                    document.title = `${to.meta.title} - VueLib`;
                }
            });

            app.use(router);

            // 5. 挂载
            app.mount('#app');
            console.log('[App] 应用启动成功');

        } catch (err) {
            console.error('[App] 应用启动失败:', err);
            appElement.innerHTML = `
                <div style="padding:40px;text-align:center;color:#f56c6c;">
                    <h2>应用启动失败</h2>
                    <p>${err.message}</p>
                    <p style="font-size:12px;color:#909399;margin-top:16px;">
                        请确认：1) SQL Server 已启动 2) 已执行 sql/ 目录下的初始化脚本 3) appsettings.json 连接字符串正确
                    </p>
                    <button onclick="location.reload()" style="margin-top:16px;padding:8px 20px;background:#409eff;color:#fff;border:none;border-radius:4px;cursor:pointer;">重试</button>
                </div>`;
        }
    }

    // 页面加载完成后启动
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }

})();

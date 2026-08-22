/* ============================================================
 * VueLib 组件库 - 模拟数据
 * 包含: 3个公共组件 + 3个页面组件(Router)
 * ============================================================ */
USE VueLib;
GO

SET IDENTITY_INSERT dbo.ComponentDefinitions ON;
GO

/* ==================== 公共组件 (ComponentType=1) ==================== */

-- 1. HelloWorld 公共组件
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (1, N'HelloWorld', 1, NULL,
     N'<div class="hello-world">
    <h2>{{ title }}</h2>
    <p>{{ message }}</p>
    <button @click="sayHello">点击问候</button>
</div>',
     N'export default {
    name: "HelloWorld",
    props: {
        title: { type: String, default: "你好，世界" },
        message: { type: String, default: "这是一个从数据库动态加载的公共组件" }
    },
    data() {
        return { count: 0 };
    },
    methods: {
        sayHello() {
            this.count++;
            alert("Hello! 已点击 " + this.count + " 次");
        }
    }
};',
     N'.hello-world { padding: 16px; border: 1px solid #42b983; border-radius: 8px; background: #f0f9eb; }
.hello-world h2 { color: #42b983; margin: 0 0 8px; }
.hello-world button { margin-top: 8px; padding: 6px 16px; background: #42b983; color: #fff; border: none; border-radius: 4px; cursor: pointer; }',
     N'基础问候组件，演示 props 与事件', 1, 1);

-- 2. Counter 公共组件
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (2, N'Counter', 1, NULL,
     N'<div class="counter-box">
    <span class="counter-label">计数器:</span>
    <button class="counter-btn" @click="decrement">-</button>
    <span class="counter-value">{{ count }}</span>
    <button class="counter-btn" @click="increment">+</button>
    <button class="counter-btn reset" @click="reset">重置</button>
</div>',
     N'export default {
    name: "Counter",
    props: {
        initial: { type: Number, default: 0 },
        step: { type: Number, default: 1 }
    },
    emits: ["change"],
    data() {
        return { count: this.initial };
    },
    methods: {
        increment() { this.count += this.step; this.$emit("change", this.count); },
        decrement() { this.count -= this.step; this.$emit("change", this.count); },
        reset() { this.count = this.initial; this.$emit("change", this.count); }
    }
};',
     N'.counter-box { display: inline-flex; align-items: center; gap: 8px; padding: 12px 16px; background: #ecf5ff; border-radius: 8px; }
.counter-label { font-weight: bold; color: #409eff; }
.counter-value { min-width: 40px; text-align: center; font-size: 20px; font-weight: bold; }
.counter-btn { width: 32px; height: 32px; border: none; border-radius: 4px; background: #409eff; color: #fff; cursor: pointer; font-size: 16px; }
.counter-btn.reset { width: auto; padding: 0 12px; background: #909399; }',
     N'计数器组件，支持步长和变更事件', 1, 2);

-- 3. UserCard 公共组件
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (3, N'UserCard', 1, NULL,
     N'<div class="user-card">
    <div class="avatar">{{ initial }}</div>
    <div class="user-info">
        <div class="user-name">{{ user.name }}</div>
        <div class="user-email">{{ user.email }}</div>
        <div class="user-role" :class="roleClass">{{ user.role }}</div>
    </div>
</div>',
     N'export default {
    name: "UserCard",
    props: {
        user: {
            type: Object,
            required: true,
            default: () => ({ name: "匿名", email: "-", role: "guest" })
        }
    },
    computed: {
        initial() { return (this.user.name || "?").charAt(0).toUpperCase(); },
        roleClass() { return "role-" + (this.user.role || "guest"); }
    }
};',
     N'.user-card { display: flex; align-items: center; gap: 12px; padding: 12px 16px; background: #fff; border: 1px solid #ebeef5; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.08); }
.avatar { width: 48px; height: 48px; border-radius: 50%; background: linear-gradient(135deg,#667eea,#764ba2); color: #fff; display: flex; align-items: center; justify-content: center; font-size: 20px; font-weight: bold; }
.user-name { font-weight: bold; font-size: 16px; }
.user-email { color: #909399; font-size: 13px; }
.user-role { display: inline-block; margin-top: 4px; padding: 2px 8px; border-radius: 10px; font-size: 12px; }
.role-admin { background: #fef0f0; color: #f56c6c; }
.role-user { background: #ecf5ff; color: #409eff; }
.role-guest { background: #f4f4f5; color: #909399; }',
     N'用户卡片组件，展示用户信息', 1, 3);

/* ==================== 页面组件 (ComponentType=2, 带 RoutePath) ==================== */

-- 4. HomePage 首页
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (4, N'HomePage', 2, N'/',
     N'<div class="page-home">
    <h1>🏠 首页 - 动态组件加载演示</h1>
    <p class="subtitle">本页面所有组件均从 SQL Server 数据库通过 API 动态加载并注册为 Vue 3 异步组件</p>

    <section class="demo-section">
        <h3>公共组件: HelloWorld</h3>
        <hello-world title="来自数据库的问候" message="组件定义存储在 ComponentDefinitions 表中"></hello-world>
    </section>

    <section class="demo-section">
        <h3>公共组件: Counter</h3>
        <counter :initial="10" :step="2" @change="onCounterChange"></counter>
        <p v-if="lastCount !== null" class="counter-result">当前值: {{ lastCount }}</p>
    </section>

    <section class="demo-section">
        <h3>公共组件: UserCard</h3>
        <user-card v-for="u in users" :key="u.id" :user="u"></user-card>
    </section>

    <section class="demo-section">
        <h3>Razor View 回退组件: RazorDemo（数据库中不存在，从 .cshtml 加载）</h3>
        <razor-demo title="Razor 回退加载成功"></razor-demo>
    </section>
</div>',
     N'export default {
    name: "HomePage",
    data() {
        return {
            lastCount: null,
            users: [
                { id: 1, name: "张三", email: "zhangsan@example.com", role: "admin" },
                { id: 2, name: "李四", email: "lisi@example.com", role: "user" },
                { id: 3, name: "王五", email: "wangwu@example.com", role: "guest" }
            ]
        };
    },
    methods: {
        onCounterChange(val) { this.lastCount = val; }
    }
};',
     N'.page-home { padding: 24px; max-width: 900px; margin: 0 auto; }
.page-home h1 { color: #303133; }
.subtitle { color: #909399; margin-bottom: 24px; }
.demo-section { margin-bottom: 24px; padding: 16px; background: #fafafa; border-radius: 8px; }
.demo-section h3 { margin-top: 0; color: #606266; }
.counter-result { margin-top: 8px; color: #67c23a; font-weight: bold; }
.user-card { margin-bottom: 8px; }',
     N'首页，演示公共组件组合使用', 1, 1);

-- 5. AboutPage 关于页
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (5, N'AboutPage', 2, N'/about',
     N'<div class="page-about">
    <h1>ℹ️ 关于本系统</h1>
    <div class="about-content">
        <h3>技术架构</h3>
        <ul>
            <li><strong>后端:</strong> ASP.NET Core 8.0 MVC + SqlSugar ORM</li>
            <li><strong>前端:</strong> Vue 3 UMD 全局构建 + Vue Router</li>
            <li><strong>组件载体:</strong> Razor View (.cshtml) 定义 template/script</li>
            <li><strong>动态加载:</strong> vueLoadCom() 将数据库组件转为 Vue 3 异步组件</li>
            <li><strong>组件分类:</strong> 公共组件 (全局注册) + 页面组件 (Router 对应)</li>
        </ul>
        <h3>加载流程</h3>
        <ol>
            <li>App 启动时从 /api/component/list 获取已启用组件清单</li>
            <li>公共组件调用 app.component() 全局注册为异步组件</li>
            <li>页面组件注册到 Vue Router 的 routes 中</li>
            <li>组件首次渲染时通过 /api/component/define/{name} 拉取完整定义</li>
            <li>运行时 eval script + 编译 template，生成真实 Vue 组件</li>
        </ol>
    </div>
</div>',
     N'export default { name: "AboutPage" };',
     N'.page-about { padding: 24px; max-width: 900px; margin: 0 auto; }
.about-content { line-height: 1.8; color: #606266; }
.about-content ul, .about-content ol { padding-left: 24px; }
.about-content li { margin-bottom: 6px; }',
     N'关于页，介绍系统架构', 1, 2);

-- 6. UserListPage 用户列表页
INSERT INTO dbo.ComponentDefinitions
    (Id, ComponentName, ComponentType, RoutePath, TemplateContent, ScriptContent, StyleContent, Description, IsEnabled, SortOrder)
VALUES
    (6, N'UserListPage', 2, N'/users',
     N'<div class="page-users">
    <h1>👥 用户列表页</h1>
    <div class="toolbar">
        <input v-model="keyword" placeholder="搜索用户名..." class="search-input" />
        <span class="total">共 {{ filteredUsers.length }} 人</span>
    </div>
    <div class="user-grid">
        <user-card v-for="u in filteredUsers" :key="u.id" :user="u"></user-card>
    </div>
    <div v-if="filteredUsers.length === 0" class="empty">未找到匹配用户</div>
</div>',
     N'export default {
    name: "UserListPage",
    data() {
        return {
            keyword: "",
            users: [
                { id: 1, name: "张三", email: "zhangsan@example.com", role: "admin" },
                { id: 2, name: "李四", email: "lisi@example.com", role: "user" },
                { id: 3, name: "王五", email: "wangwu@example.com", role: "guest" },
                { id: 4, name: "赵六", email: "zhaoliu@example.com", role: "user" },
                { id: 5, name: "钱七", email: "qianqi@example.com", role: "admin" }
            ]
        };
    },
    computed: {
        filteredUsers() {
            const kw = this.keyword.trim().toLowerCase();
            if (!kw) return this.users;
            return this.users.filter(u =>
                u.name.toLowerCase().includes(kw) || u.email.toLowerCase().includes(kw)
            );
        }
    }
};',
     N'.page-users { padding: 24px; max-width: 900px; margin: 0 auto; }
.toolbar { display: flex; align-items: center; gap: 16px; margin-bottom: 16px; }
.search-input { flex: 1; padding: 8px 12px; border: 1px solid #dcdfe6; border-radius: 4px; font-size: 14px; }
.total { color: #909399; font-size: 13px; }
.user-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }
.empty { text-align: center; color: #909399; padding: 40px; }',
     N'用户列表页，演示搜索过滤与 UserCard 复用', 1, 3);

SET IDENTITY_INSERT dbo.ComponentDefinitions OFF;
GO

PRINT '模拟数据插入完成，共 6 条组件记录 (3公共 + 3页面)。';
GO

-- 验证查询
SELECT Id, ComponentName,
       CASE ComponentType WHEN 1 THEN N'公共组件' WHEN 2 THEN N'页面组件' END AS ComponentType,
       RoutePath, IsEnabled, SortOrder
FROM dbo.ComponentDefinitions
ORDER BY ComponentType, SortOrder;
GO

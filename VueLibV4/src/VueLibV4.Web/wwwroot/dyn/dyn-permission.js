/**
 * dyn-permission.js — 前端权限 UI 控制
 *
 * 用法：
 *   1. 页面加载后自动 fetch /api/platform/permission/me，结果存 sessionStorage
 *   2. 扫描所有带 data-permission-op / data-permission-field 的 DOM 元素
 *   3. 无权限的元素自动 disable 或隐藏（data-permission-mode="hide" 隐藏，默认 disable）
 *
 * 资源上下文查找：
 *   - 按钮只写操作类型：data-permission-op="delete"
 *   - 向上遍历 DOM 找 data-block-resource / data-webpage-resource
 *   - 拼出完整权限：Customer|Delete
 *   - 找不到资源上下文 = 页面未配置资源节点，与后端一致【放行】（无法判定不拦 UI，后端兜底）
 *
 * 弹窗透传：
 *   - ActionHelper 打开弹窗时，如果配置 inheritResource:true，自动把当前 resourceKey 写到弹窗根节点
 *   - 弹窗渲染完成后调 DynPermission.rescan() 重新扫描
 *
 * 身份切换：
 *   - /me 返回 401 时立即清空缓存（防止匿名/上一用户的 admin 缓存串号）
 *   - 登录页、logout、dyn-call 的 401 拦截都会调 clearCache()
 *
 * 注意：前端只做 UI 体验控制，真正的安全校验在后端 PermissionFilter。
 */
(function () {
  const KEY = 'dyn_permission_keys';
  let keys = new Set();
  let isAdmin = false;

  function clearCache() {
    try { sessionStorage.removeItem(KEY); } catch (e) { /* 隐私模式等场景忽略 */ }
    keys = new Set();
    isAdmin = false;
  }

  async function load() {
    try {
      // 先读缓存
      const cached = sessionStorage.getItem(KEY);
      if (cached) {
        const data = JSON.parse(cached);
        keys = new Set(data.keys || []);
        isAdmin = !!data.isAdmin;
        scan();
      }
      // 再拉新的
      const res = await fetch('/api/platform/permission/me');
      if (res.status === 401) {
        // 未登录/票据失效：缓存可能是匿名或上一个用户的，必须丢弃（交由 dyn-call 统一跳登录）
        clearCache();
        return;
      }
      if (!res.ok) return;
      const json = await res.json();
      const data = json.data || {};
      keys = new Set(data.keys || []);
      isAdmin = !!data.isAdmin;
      sessionStorage.setItem(KEY, JSON.stringify(data));
      scan();
    } catch (e) {
      console.warn('[dyn-permission] 加载权限失败', e);
    }
  }

  // 向上 DOM 查找 resourceKey
  function findResourceKey(el) {
    let current = el;
    while (current && current !== document) {
      const resKey = current.getAttribute && (
        current.getAttribute('data-block-resource') ||
        current.getAttribute('data-webpage-resource')
      );
      if (resKey) return resKey;
      current = current.parentElement;
    }
    return null;
  }

  // 有资源上下文时按 key+级别判定；level 缺省时按裸 key 精确匹配
  function hasPermission(key, level) {
    if (isAdmin) return true;
    if (!key) return true;
    if (!level) return keys.has(key);
    if (level === 'Read') return keys.has(key) || keys.has(key + '|Read') || keys.has(key + '|Edit');
    if (level === 'Edit') return keys.has(key + '|Edit');
    if (level === 'Delete') return keys.has(key + '|Delete');
    return keys.has(key);
  }

  // 把 "read"/"edit"/"delete" 转成首字母大写
  function cap(s) { return s ? s.charAt(0).toUpperCase() + s.slice(1).toLowerCase() : s; }

  function scan(root) {
    root = root || document.body;
    // 操作权限
    root.querySelectorAll('[data-permission-op]').forEach(el => {
      const op = el.getAttribute('data-permission-op');
      const resourceKey = findResourceKey(el);
      // 无资源上下文：页面未绑定资源节点，按"未配置不阻塞"放行（与后端 CheckTable 语义一致）；
      // 有资源上下文：拼 resourceKey|Level 判定。绝不能把完整 Key 再拼后缀（旧 bug 会查 delete|Delete）。
      const allowed = resourceKey ? hasPermission(resourceKey, cap(op)) : true;
      if (!allowed) {
        const mode = el.getAttribute('data-permission-mode') || 'disable';
        if (mode === 'hide') {
          el.style.display = 'none';
        } else {
          el.setAttribute('disabled', 'disabled');
          el.classList.add('is-disabled');
          el.style.opacity = '0.5';
        }
      } else {
        el.removeAttribute('disabled');
        el.classList.remove('is-disabled');
        el.style.opacity = '';
        if (el.getAttribute('data-permission-mode') === 'hide') el.style.display = '';
      }
    });
    // 字段权限
    root.querySelectorAll('[data-permission-field]').forEach(el => {
      const field = el.getAttribute('data-permission-field');
      const resourceKey = findResourceKey(el);
      // 无资源上下文同样放行；有上下文时按 resourceKey.field 裸 key 精确匹配（不再强套 Edit 后缀）
      const allowed = resourceKey ? hasPermission(resourceKey + '.' + field) : true;
      // 字段级：未授权就只读
      if (!allowed) {
        el.setAttribute('readonly', 'readonly');
        el.classList.add('is-readonly');
      } else {
        el.removeAttribute('readonly');
        el.classList.remove('is-readonly');
      }
    });
  }

  // 登出：调后端清 cookie + 本地清缓存 + 跳登录页（供未来顶栏登出入口调用）
  async function logout() {
    try { await fetch('/api/platform/permission/logout', { method: 'POST' }); } catch (e) { /* 忽略网络异常继续跳登录 */ }
    clearCache();
    location.href = '/Platform/Page/Login';
  }

  // 暴露给外部
  window.DynPermission = {
    load,
    scan,
    rescan: scan,
    findResourceKey,
    has: (key, level) => hasPermission(key, level),
    refresh: load,
    clearCache,
    logout
  };

  // DOM 就绪后自动加载
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', load);
  } else {
    load();
  }
})();

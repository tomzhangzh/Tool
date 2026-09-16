/* ============================================================
 * VueLibV3  dyn-lib.js  (UMD 动态库加载器)
 * ------------------------------------------------------------
 * 职责：
 *   1. 按顺序加载默认依赖：vue / element-plus / element-icons /
 *      lodash / layui / tailwind.css / sortable / echarts / codemirror
 *   2. 再加载 dyn 模块：dyn-validator → dyn-actionhelper → dyn-core
 *      → dyn-template → dyn-com
 *   3. 全部就绪后触发 DynLib.ready(cb)
 * 用法：
 *   <script src="/dyn/dyn-lib.js"></script>
 *   <script>
 *     DynLib.ready(function(){
 *       var app = Vue.createApp({ ... });
 *       DynCom.setupApp(app);
 *       app.mount('#app');
 *     });
 *   </script>
 * 静态变量统一放在文件头部。
 * ============================================================ */
(function (global) {
  'use strict';

  /* ---------- 静态变量（头部集中） ---------- */
  var VERSION = '3.0.0';
  var BASE = (function () {
    var scripts = document.getElementsByTagName('script');
    var src = scripts[scripts.length - 1].src || '';
    return src.substring(0, src.lastIndexOf('/') + 1);
  })();
  var DEFAULT_LIBS = [
    { url: 'vue.global.prod.js', name: 'vue' },
    { url: 'element-plus/index.css', name: 'element-plus-css' },
    { url: 'element-plus/index.full.min.js', name: 'element-plus' },
    { url: 'element-plus/icons.min.js', name: 'element-plus-icons' },
    { url: 'lodash.min.js', name: 'lodash' },
    { url: 'layui/css/layui.css', name: 'layui-css' },
    { url: 'layui/layui.js', name: 'layui' },
    { url: 'tailwind.css', name: 'tailwind' },
    { url: 'sortable.min.js', name: 'sortable' },
    { url: 'echarts.min.js', name: 'echarts' },
    { url: 'codemirror/codemirror.min.css', name: 'codemirror-css' },
    { url: 'codemirror/codemirror.min.js', name: 'codemirror' },
    { url: 'codemirror/mode-javascript.min.js', name: 'cm-js' },
    { url: 'codemirror/mode-htmlmixed.min.js', name: 'cm-html' },
    { url: 'codemirror/mode-xml.min.js', name: 'cm-xml' },
    { url: 'codemirror/mode-css.min.js', name: 'cm-css' },
    { url: 'codemirror/mode-sql.min.js', name: 'cm-sql' }
  ];
  var DYN_MODULES = [
    'dyn-validator.js',
    'dyn-actionhelper.js',
    'dyn-core.js',
    'dyn-template.js',
    'dyn-com.js',
    'dyn-designer.js'
  ];

  /* ---------- DynLib 对象 ---------- */
  var DynLib = {
    version: VERSION,
    base: BASE,
    _libs: {},
    _ready: false,
    _queue: [],

    /** 相对 dyn/ 目录的脚本路径 */
    script: function (rel) { return BASE + rel; },
    /** lib 目录路径 */
    lib: function (rel) { return BASE + '../lib/' + rel; },

    use: function (name) { return this._libs[name] ? global[name] : null; },

    /** 动态加载单个资源（脚本或样式），可重复调用 */
    load: function (url) {
      var self = this;
      if (this._libs[url]) return Promise.resolve(true);
      return new Promise(function (resolve, reject) {
        var el;
        if (/\.css($|\?)/.test(url)) {
          el = document.createElement('link');
          el.rel = 'stylesheet';
          el.href = url;
          el.onload = function () { self._libs[url] = true; resolve(true); };
          el.onerror = function () { reject(new Error('[DynLib] 样式加载失败: ' + url)); };
        } else {
          el = document.createElement('script');
          el.src = url;
          el.async = false;
          el.onload = function () { self._libs[url] = true; resolve(true); };
          el.onerror = function () { reject(new Error('[DynLib] 脚本加载失败: ' + url)); };
        }
        document.head.appendChild(el);
      });
    },

    /** 依赖就绪后执行回调 */
    ready: function (cb) {
      if (this._ready) { cb(); return; }
      this._queue.push(cb);
    },

    _fireReady: function () {
      this._ready = true;
      var q = this._queue;
      this._queue = [];
      q.forEach(function (cb) {
        try { cb(); } catch (e) { console.error('[DynLib] ready 回调异常', e); }
      });
    }
  };
  global.DynLib = DynLib;

  /* ---------- 默认加载链 ---------- */
  var chain = DEFAULT_LIBS.map(function (item) {
    return { url: DynLib.lib(item.url), name: item.name };
  }).concat(DYN_MODULES.map(function (m) {
    return { url: DynLib.script(m), name: m };
  }));

  chain.reduce(function (p, item) {
    return p.then(function () { return DynLib.load(item.url); });
  }, Promise.resolve())
    .then(function () {
      DynLib._fireReady();
    })
    .catch(function (err) {
      console.error('[DynLib] 依赖加载失败', err);
      DynLib._fireReady(); // 尽量继续，个别库缺失由各模块自行降级
    });
})(window);

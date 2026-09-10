/**
 * dyn-lib.js —— dyn 入口装配（拆分后的兼容层）
 * ----------------------------------------------------------------------------
 * 拆分结构：
 *   dyn-core.js       框架核心（工具/挂载/刷新/回发/模态/事件总线/i18n/静态配置）
 *   dyn-actionhelper.js 动作系统 + 动作助手（委托/内置动作/新动作/DB 助手/META）
 *   dyn-lib.js        入口装配（本文件）
 *
 * 兼容性：
 *   - 新页面按顺序引入 dyn-core.js → dyn-actionhelper.js → dyn-lib.js
 *   - 旧页面只引入 dyn-lib.js 也能工作（document.write 同步注入两个依赖）
 * ----------------------------------------------------------------------------
 */
(function (global) {
    'use strict';

    var SRC_DIR = (function () {
        var s = document.currentScript || (function () {
            var sc = document.getElementsByTagName('script');
            return sc[sc.length - 1];
        })();
        var src = (s && s.src) || '';
        return src.substring(0, src.lastIndexOf('/') + 1);
    })();

    function ensure(globalName, fileName) {
        if (global[globalName]) return;
        document.write('<script src="' + SRC_DIR + fileName + '.js"><\/script>');
    }

    // 依赖缺失时同步注入（document.write 仅用于解析期同步加载）
    ensure('dynCore', 'dyn-core');
    ensure('dynActionHelper', 'dyn-actionhelper');

    // 最终装配：window.dyn = dynCore（动作系统已由 dyn-actionhelper.js 注入）
    if (global.dynCore) global.dyn = global.dynCore;
})(window);

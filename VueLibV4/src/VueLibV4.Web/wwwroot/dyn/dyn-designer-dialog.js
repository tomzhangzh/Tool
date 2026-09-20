/* ============================================================
 * VueLibV4  dyn-designer-dialog.js  设计器弹窗（M3-3）
 * 依赖：dyn-action.js（layer）、dyn-designer-ops.js（页面内清理 JSON 由设计器子页自行调用）
 *
 * 用法：
 *   DynDesignerDialog.open({
 *     configJson: cfg,          // 初始配置树（可空 → 空白栅格页）
 *     defaultModel: {},         // 初始默认数据模型
 *     title: '编辑页面配置',
 *     width: '92%', height: '92%'
 *   }).then(function (res) {
 *     if (!res) return;          // 用户取消
 *     res.configJson;            // 清理后的配置树对象
 *     res.defaultModel;          // 默认数据模型对象
 *   });
 *
 * 协议（子页 = /Platform/Page/Designer?embed=1）：
 *   子 → 父 {type:'dyn-designer-ready'}
 *   父 → 子 {type:'dyn-designer-init', configJson, defaultModel, title, pageId, pageCode}
 *   子 → 父 {type:'dyn-designer-result', configJson, defaultModel}  → resolve 并关闭
 *   子 → 父 {type:'dyn-designer-cancel'}                            → resolve(null) 并关闭
 * ============================================================ */
(function (global) {
  'use strict';

  var DESIGNER_URL = '/Platform/Page/Designer?embed=1';

  function getLayer() {
    return new Promise(function (resolve) {
      if (global.layui && global.layui.layer) return resolve(global.layui.layer);
      if (global.layui && typeof global.layui.use === 'function') {
        global.layui.use(['layer'], function () { resolve(global.layui.layer || null); });
        return;
      }
      if (global.dyn && typeof global.dyn.getLayer === 'function') return global.dyn.getLayer().then(resolve);
      resolve(null);
    });
  }

  var DesignerDialog = {
    /**
     * 打开设计器弹窗
     * @param {object} opts {configJson,defaultModel,title,width,height,url,pageId,pageCode}
     * @returns {Promise<{configJson:object,defaultModel:object}|null>}
     */
    open: function (opts) {
      opts = opts || {};
      return getLayer().then(function (layer) {
        if (!layer) return Promise.reject(new Error('layui layer 不可用'));
        return new Promise(function (resolve) {
          var settled = false;
          var index = null;
          var readySent = false;

          function cleanup() {
            global.removeEventListener('message', onMessage);
          }
          function finish(data) {
            if (settled) return;
            settled = true;
            cleanup();
            try { layer.close(index); } catch (e) {}
            resolve(data);
          }

          function onMessage(e) {
            var d = e.data || {};
            if (d.type === 'dyn-designer-ready' && !readySent) {
              readySent = true;
              var iframe = document.querySelector('.layui-layer[times="' + index + '"] iframe')
                || document.getElementById('layui-layer-iframe' + index);
              var win = null;
              try {
                // 优先用 layer 回调中保存的 iframe 元素
                win = (iframe && iframe.contentWindow) || (lastIframe && lastIframe.contentWindow);
              } catch (err) { win = null; }
              if (!win) {
                // 兜底：从最新打开的 layer iframe 集合中取最后一个
                var list = document.querySelectorAll('.layui-layer-iframe iframe');
                win = list.length ? list[list.length - 1].contentWindow : null;
              }
              if (win) {
                win.postMessage({
                  type: 'dyn-designer-init',
                  configJson: opts.configJson || null,
                  defaultModel: opts.defaultModel || {},
                  title: opts.title || '页面配置设计器',
                  pageId: opts.pageId || 0,
                  pageCode: opts.pageCode || ''
                }, '*');
              }
            } else if (d.type === 'dyn-designer-result') {
              finish({ configJson: d.configJson || null, defaultModel: d.defaultModel || {} });
            } else if (d.type === 'dyn-designer-cancel') {
              finish(null);
            }
          }

          var lastIframe = null;
          global.addEventListener('message', onMessage);

          index = layer.open({
            type: 2,
            title: opts.title || '页面配置设计器',
            area: [opts.width || '92%', opts.height || '92%'],
            shadeClose: false,
            content: opts.url || DESIGNER_URL,
            success: function (layero) {
              lastIframe = layero.find('iframe')[0] || null;
            },
            end: function () {
              // 右上角 X / ESC 关闭视为取消
              if (!settled) { settled = true; cleanup(); resolve(null); }
            }
          });
        });
      });
    }
  };

  global.DynDesignerDialog = DesignerDialog;
})(window);

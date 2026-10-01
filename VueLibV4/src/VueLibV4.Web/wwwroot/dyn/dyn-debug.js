/* dyn-debug.js — Dyn 体系调试护栏（?dyndebug=1）
 * ------------------------------------------------------------
 * 启用方式（任一）：
 *   1) URL 带 ?dyndebug=1
 *   2) localStorage.DYN_DEBUG = '1'
 *   3) window.DYN_DEBUG = true（需在 dyn-lib 之前设置）
 *
 * 能力：
 *   - 页面右下角浮层，列出当前全部 Block（role / 已注册命令 / 已订阅事件）
 *   - 每个 Block 的关键参数最终值 + 获胜层（table/loadUrl/filterInit/rowId/project...）
 *     —— 串表/预置丢失类问题一眼定位，不用再手写 inspect
 *   - 参数链拓扑：每个 ctx 的 id 与父链（L1 声明遮蔽 bug 直接可见）
 *   - Notes：收集运行期告警（未知 op、未注册命令等），带来源与时间
 *   - window.DynDebug.note(msg, meta) 供各模块统一上报
 *
 * 零依赖、原生 DOM；样式自带注入，不污染业务 css。
 */
(function (global) {
    'use strict';

    function enabled() {
        try {
            if (global.DYN_DEBUG === true) return true;
            if (global.localStorage && localStorage.getItem('DYN_DEBUG') === '1') return true;
            var s = global.location && global.location.search || '';
            return /[?&]dyndebug=1\b/.test(s);
        } catch (e) { return false; }
    }

    var notes = [];
    var panelEl = null;
    var bodyEl = null;
    var collapsed = false;
    var timer = null;

    var DynDebug = {
        enabled: enabled(),
        /** 上报一条调试记录：msg 文本，meta={source,table,...} */
        note: function (msg, meta) {
            if (!this.enabled) return;
            notes.push({ t: new Date(), msg: String(msg == null ? '' : msg), meta: meta || {} });
            if (notes.length > 200) notes.shift();
            this.scheduleRender();
        },
        clear: function () { notes.length = 0; this.render(); },
        /** 立即扫描渲染 */
        render: function () { if (panelEl) draw(); },
        scheduleRender: function () {
            if (!panelEl) return;
            if (renderQueued) return;
            renderQueued = true;
            setTimeout(function () { renderQueued = false; draw(); }, 200);
        }
    };
    var renderQueued = false;
    global.DynDebug = DynDebug;

    if (!DynDebug.enabled) return;

    var WATCH_KEYS = ['table', 'tableName', 'keyField', 'project', 'loadUrl', 'deleteUrl',
        'addUrl', 'editUrl', 'filterInit', 'rowId', 'preFill', 'modalPageId'];

    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function fmtVal(v) {
        if (v === undefined) return '<span class="dyndb-na">undefined</span>';
        if (v === null) return '<span class="dyndb-na">null</span>';
        var s;
        try { s = typeof v === 'string' ? v : JSON.stringify(v); } catch (e) { s = String(v); }
        if (s.length > 120) s = s.slice(0, 120) + '…';
        return '<span class="dyndb-val">' + esc(s) + '</span>';
    }

    function layerClass(layer) {
        if (!layer) return 'dyndb-l0';
        if (layer.indexOf('L0') === 0) return 'dyndb-l0';
        if (layer.indexOf('L1') === 0) return 'dyndb-l1';
        if (layer.indexOf('L2') === 0) return 'dyndb-l2';
        if (layer.indexOf('L3') === 0 || layer.indexOf('L4') === 0) return 'dyndb-l34';
        return 'dyndb-shared';
    }

    function blockSection(el, idx) {
        var role = el.getAttribute('data-blk-role') || ('block#' + idx);
        var h = el.__dynBlock;
        var intro = h && h.introspect ? h.introspect() : null;
        var html = '<div class="dyndb-card">';
        html += '<div class="dyndb-card-h"><b>' + esc(role) + '</b>'
            + (el.id ? ' <span class="dyndb-id">#' + esc(el.id) + '</span>' : '')
            + (intro && intro.destroyed ? ' <span class="dyndb-bad">destroyed</span>' : '')
            + (!h ? ' <span class="dyndb-bad">未挂载 __dynBlock</span>' : '')
            + '</div>';

        if (intro) {
            html += '<div class="dyndb-line"><span class="dyndb-k">cmds</span> '
                + (intro.commands.length ? intro.commands.map(esc).join(', ') : '<span class="dyndb-na">（无）</span>') + '</div>';
            if (intro.events.length)
                html += '<div class="dyndb-line"><span class="dyndb-k">events</span> ' + intro.events.map(esc).join(', ') + '</div>';
        }

        // 关键参数：值 + 获胜层
        try {
            var P = global.DynParams && DynParams.fromEl(el);
            if (P) {
                var insp = P.inspect(WATCH_KEYS);
                var rows = WATCH_KEYS.filter(function (k) { return insp[k] && insp[k].value !== undefined; });
                if (rows.length) {
                    html += '<div class="dyndb-params">';
                    rows.forEach(function (k) {
                        var r = insp[k];
                        html += '<div class="dyndb-line"><span class="dyndb-k">' + esc(k) + '</span> '
                            + fmtVal(r.value)
                            + ' <span class="dyndb-layer ' + layerClass(r.layer) + '">' + esc(r.layer || '?') + '</span></div>';
                    });
                    html += '</div>';
                }
                // 链拓扑：从本 ctx 向上
                var chain = [];
                var c = P;
                var guard = 0;
                while (c && guard++ < 20) {
                    var nLocal = Object.keys(c.local || {}).length;
                    var nShared = Object.keys(c.shared || {}).length;
                    var tag = c.id;
                    if (nLocal) tag += ' [L1×' + nLocal + ']';
                    if (nShared) tag += ' [sh×' + nShared + ']';
                    chain.push(tag);
                    c = c.parent;
                }
                html += '<div class="dyndb-line dyndb-chain"><span class="dyndb-k">ctx链</span> '
                    + esc(chain.join(' → ')) + '</div>';
            }
        } catch (e) {
            html += '<div class="dyndb-line dyndb-bad">inspect 异常: ' + esc(e.message) + '</div>';
        }
        html += '</div>';
        return html;
    }

    function draw() {
        if (!bodyEl) return;
        var blocks = [];
        try { blocks = [].slice.call(document.querySelectorAll('[data-blk-role]')); } catch (e) { }
        var blocksHtml = blocks.length
            ? blocks.map(blockSection).join('')
            : '<div class="dyndb-empty">当前页面没有已渲染的 Block</div>';

        var notesHtml = notes.length
            ? notes.slice().reverse().map(function (n) {
                var hh = ('0' + n.t.getHours()).slice(-2), mm = ('0' + n.t.getMinutes()).slice(-2), ss = ('0' + n.t.getSeconds()).slice(-2);
                return '<div class="dyndb-note"><span class="dyndb-time">' + hh + ':' + mm + ':' + ss + '</span> '
                    + (n.meta.source ? '<span class="dyndb-src">' + esc(n.meta.source) + '</span> ' : '')
                    + esc(n.msg) + '</div>';
            }).join('')
            : '<div class="dyndb-empty">暂无告警</div>';

        bodyEl.innerHTML =
            '<div class="dyndb-sec-h">Blocks（' + blocks.length + '）</div>' + blocksHtml
            + '<div class="dyndb-sec-h">Notes（' + notes.length + '）</div>' + notesHtml;
    }

    function injectCss() {
        if (document.getElementById('dyndb-style')) return;
        var css = ''
            + '.dyndb-panel{position:fixed;right:10px;bottom:10px;width:420px;max-height:60vh;z-index:2147483600;'
            + 'font:12px/1.5 Consolas,Menlo,monospace;background:#1e1e1e;color:#d4d4d4;'
            + 'border:1px solid #454545;border-radius:8px;box-shadow:0 6px 24px rgba(0,0,0,.45);display:flex;flex-direction:column;overflow:hidden}'
            + '.dyndb-head{display:flex;align-items:center;gap:8px;padding:6px 10px;background:#2d2d30;cursor:pointer;user-select:none}'
            + '.dyndb-head b{color:#4ec9b0;letter-spacing:.5px}.dyndb-head .dyndb-spacer{flex:1}'
            + '.dyndb-btn{background:#3c3c3c;color:#ddd;border:1px solid #555;border-radius:4px;padding:1px 8px;cursor:pointer;font:12px Consolas}'
            + '.dyndb-btn:hover{background:#4a4a4a}'
            + '.dyndb-body{overflow:auto;padding:8px 10px}.dyndb-panel.dyndb-collapsed .dyndb-body{display:none}'
            + '.dyndb-sec-h{color:#569cd6;font-weight:bold;margin:8px 0 4px;border-bottom:1px solid #3a3a3a;padding-bottom:2px}'
            + '.dyndb-card{border:1px solid #3a3a3a;border-radius:5px;padding:6px 8px;margin-bottom:6px;background:#252526}'
            + '.dyndb-card-h b{color:#dcdcaa}.dyndb-id{color:#808080}'
            + '.dyndb-line{margin:2px 0;word-break:break-all}.dyndb-k{display:inline-block;min-width:78px;color:#9cdcfe}'
            + '.dyndb-val{color:#ce9178}.dyndb-na{color:#6a6a6a}'
            + '.dyndb-layer{padding:0 5px;border-radius:3px;font-size:11px;margin-left:4px}'
            + '.dyndb-l0{background:#4e2a1e;color:#f48771}.dyndb-l1{background:#143d2e;color:#4ec9b0}'
            + '.dyndb-l2{background:#2d2350;color:#c586c0}.dyndb-l34{background:#4a3a10;color:#d7ba7d}'
            + '.dyndb-shared{background:#0e3a4a;color:#569cd6}'
            + '.dyndb-chain{color:#808080}.dyndb-bad{color:#f48771;font-weight:bold}'
            + '.dyndb-note{border-left:3px solid #f48771;padding:2px 6px;margin:3px 0;background:#2a2020}'
            + '.dyndb-time{color:#808080}.dyndb-src{color:#c586c0}'
            + '.dyndb-empty{color:#6a6a6a;padding:4px 0}';
        var st = document.createElement('style');
        st.id = 'dyndb-style';
        st.textContent = css;
        document.head.appendChild(st);
    }

    function mount() {
        if (panelEl || !document.body) return;
        injectCss();
        panelEl = document.createElement('div');
        panelEl.className = 'dyndb-panel';
        panelEl.innerHTML =
            '<div class="dyndb-head" data-act="toggle"><b>dyn-debug</b>'
            + '<span class="dyndb-spacer"></span>'
            + '<button class="dyndb-btn" data-act="refresh">刷新</button>'
            + '<button class="dyndb-btn" data-act="clear">清空</button>'
            + '<button class="dyndb-btn" data-act="hide">－</button></div>'
            + '<div class="dyndb-body"></div>';
        document.body.appendChild(panelEl);
        bodyEl = panelEl.querySelector('.dyndb-body');

        panelEl.addEventListener('click', function (e) {
            var act = e.target && e.target.getAttribute && e.target.getAttribute('data-act');
            if (act === 'refresh') { draw(); return; }
            if (act === 'clear') { DynDebug.clear(); return; }
            if (act === 'hide') { panelEl.style.display = 'none'; remountBtn(); return; }
            if (act === 'toggle' && e.target.classList.contains('dyndb-head')) {
                collapsed = !collapsed;
                panelEl.classList.toggle('dyndb-collapsed', collapsed);
            }
        });

        // 动态片段（弹窗/tabs）挂载后自动刷新；2s 轻轮询，开销仅在调试模式
        timer = setInterval(draw, 2000);
        draw();
        console.info('%c[dyn-debug] 已启用：?dyndebug=1（Block 参数获胜层/告警浮层）', 'color:#4ec9b0');
    }

    function remountBtn() {
        var b = document.createElement('button');
        b.textContent = 'dyn';
        b.style.cssText = 'position:fixed;right:10px;bottom:10px;z-index:2147483600;padding:2px 10px;'
            + 'background:#2d2d30;color:#4ec9b0;border:1px solid #454545;border-radius:6px;cursor:pointer;font:12px Consolas';
        b.onclick = function () { b.remove(); panelEl.style.display = 'flex'; };
        document.body.appendChild(b);
    }

    function boot() {
        if (global.DynLib && typeof global.DynLib.ready === 'function') {
            global.DynLib.ready(function () { setTimeout(mount, 300); });
        } else if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', mount);
        } else {
            mount();
        }
    }
    boot();
})(window);

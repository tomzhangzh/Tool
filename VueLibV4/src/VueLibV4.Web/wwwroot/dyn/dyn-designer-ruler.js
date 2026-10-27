/* dyn-designer-ruler.js 设计器标尺（纯函数 + canvas 绘制）
 * layout(opts) 纯计算，可在 node 里直接测：
 *   opts: { size, scroll, zoom, unit, offset }
 *   返回 { main:[{pos,value}], sub:[pos...], step }
 * draw(canvas, wrap, opts) 负责 DPR 适配、底色、刻度与文字：
 *   opts: { vertical, zoom, scroll, unit }
 */
(function (global) {
  'use strict';

  function layout(opts) {
    var o = opts || {};
    var zoom = o.zoom > 0 ? o.zoom : 1;
    var unit = o.unit > 0 ? o.unit : 50;
    var size = o.size || 0;
    var scroll = o.scroll || 0;
    var step = unit * zoom;
    if (!(step > 0)) step = 1;
    var start = Math.floor(scroll / step) * step;
    var main = [];
    var sub = [];
    for (var x = start; x <= scroll + size; x += step) {
      main.push({ pos: Math.round(x - scroll) + 0.5, value: Math.round((x / zoom) / unit) * unit });
    }
    var subStep = step / 5;
    if (!(subStep > 0)) subStep = step;
    for (var s = start; s <= scroll + size; s += subStep) {
      sub.push(Math.round(s - scroll) + 0.5);
    }
    return { main: main, sub: sub, step: step };
  }

  function draw(canvas, wrap, opts) {
    if (!canvas || !wrap) return;
    var o = opts || {};
    var vertical = !!o.vertical;
    var zoom = o.zoom > 0 ? o.zoom : 1;
    var unit = o.unit > 0 ? o.unit : 50;
    var scroll = o.scroll || 0;
    var cssW = wrap.clientWidth || (vertical ? 20 : 0);
    var cssH = wrap.clientHeight || (vertical ? 0 : 20);
    var dpr = global.devicePixelRatio || 1;
    canvas.width = Math.max(1, Math.round(cssW * dpr));
    canvas.height = Math.max(1, Math.round(cssH * dpr));
    canvas.style.width = cssW + 'px';
    canvas.style.height = cssH + 'px';
    var ctx = canvas.getContext('2d');
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, cssW, cssH);
    ctx.fillStyle = '#f3f4f6';
    ctx.fillRect(0, 0, cssW, cssH);

    var res = layout({ size: vertical ? cssH : cssW, scroll: scroll, zoom: zoom, unit: unit });
    ctx.lineWidth = 1;
    ctx.font = (vertical ? '9px' : '10px') + ' sans-serif';
    ctx.textBaseline = 'top';

    ctx.strokeStyle = '#c0c4cc';
    ctx.fillStyle = '#5a5a5a';
    for (var i = 0; i < res.main.length; i++) {
      var t = res.main[i];
      ctx.beginPath();
      if (vertical) {
        ctx.moveTo(0, t.pos); ctx.lineTo(cssW, t.pos);
        ctx.stroke();
        ctx.fillText(String(t.value), 2, t.pos + 2);
      } else {
        ctx.moveTo(t.pos, 0); ctx.lineTo(t.pos, cssH);
        ctx.stroke();
        ctx.fillText(String(t.value), t.pos + 3, 3);
      }
    }

    ctx.strokeStyle = '#d5d8dd';
    for (var j = 0; j < res.sub.length; j++) {
      var p = res.sub[j];
      ctx.beginPath();
      if (vertical) {
        ctx.moveTo(cssW - 6, p); ctx.lineTo(cssW, p);
      } else {
        ctx.moveTo(p, cssH - 6); ctx.lineTo(p, cssH);
      }
      ctx.stroke();
    }
  }

  var api = { layout: layout, draw: draw };
  global.DynDesignerRuler = api;
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
})(typeof window !== 'undefined' ? window : globalThis);

/* dyn-designer-lint.js 设计器配置体检
 * DynDesignerOps.lintPageJson(scope) → { issues:[{level,code,message,uid}], errors }
 * 只做「保存前能拦住」的静态检查，不执行任何配置代码：
 *   错误：缺 component / 未知组件 / 非容器带子节点 / modelname 重复 / 表达式语法错误
 *   提示：空页面 / 必填规则缺 modelname / 绑定表达式花括号不配对
 */
(function (global) {
  'use strict';
  var __plugin = {
    name: 'designer-lint', stage: 'designer', requires: ['DynDesignerOps'],
    setup: function () {
      'use strict';
      const Ops = global.DynDesignerOps;

      function lintPageJson(scope) {
        const issues = [];
        if (!scope || !scope.pageJson) {
          return { issues: [{ level: 'error', code: 'empty', message: '页面配置为空', uid: '' }], errors: 1 };
        }
        const metaReady = (scope.componentMetaList || []).length > 0;
        const seen = {};

        function add(level, code, message, uid) {
          issues.push({ level: level, code: code, message: message, uid: uid || '' });
        }
        // 仅编译不执行：捕获 evalcondition / 校验表达式的语法错误
        function checkExpr(text, label, uid) {
          if (!text || typeof text !== 'string' || !text.trim()) return;
          try { new Function('ctx', text); }
          catch (e) { add('error', 'expr', label + '语法错误：' + e.message, uid); }
        }
        function checkBraces(text, label, uid) {
          if (typeof text !== 'string') return;
          const open = (text.match(/{{/g) || []).length;
          const close = (text.match(/}}/g) || []).length;
          if (open !== close) add('warning', 'binding', label + '花括号不配对', uid);
        }

        function walk(node, isRoot) {
          if (!node || typeof node !== 'object') return;
          const uid = Ops.uidOf(node);
          const comp = node.component || '';
          if (!comp) add('error', 'component', '存在缺少 component 的节点', uid);
          else if (metaReady && !Ops.metaOf(comp).ComponentName) add('error', 'unknown', '未知组件：' + comp, uid);

          const mn = String(node.modelname || '').trim();
          if (mn) {
            if (seen[mn]) add('error', 'dupe', 'modelname 重复：' + mn, uid);
            else seen[mn] = uid;
          }

          const kids = Array.isArray(node.childrenctrls) ? node.childrenctrls : [];
          if (kids.length && comp && !Ops.isContainer(comp)) {
            add('error', 'children', '非容器组件不能有子节点：' + comp, uid);
          }
          if (isRoot && !kids.length) add('warning', 'empty', '页面没有任何组件', uid);

          const validators = Array.isArray(node.validators) ? node.validators : [];
          validators.forEach(function (v) {
            if (!v || typeof v !== 'object') return;
            const isRequired = v.required === true || v.type === 'required' || v.rule === 'required';
            if (isRequired && !mn) add('warning', 'required', '必填校验缺少 modelname', uid);
            checkExpr(v.expression || v.expr, '校验表达式', uid);
          });
          checkExpr(node.evalcondition, '显示条件 evalcondition', uid);
          checkExpr(node.modelexpr, 'modelexpr', uid);
          checkExpr(node.modelsetexpr, 'modelsetexpr', uid);
          if (node.bindings && typeof node.bindings === 'object') {
            Object.keys(node.bindings).forEach(function (k) {
              checkBraces(node.bindings[k], '绑定 ' + k, uid);
            });
          }

          kids.forEach(function (c) { walk(c, false); });
          const slots = node.slots || {};
          Object.keys(slots).forEach(function (k) {
            const arr = Array.isArray(slots[k]) ? slots[k] : [];
            arr.forEach(function (c) { walk(c, false); });
          });
        }

        walk(scope.pageJson, true);
        return {
          issues: issues,
          errors: issues.filter(function (x) { return x.level === 'error'; }).length
        };
      }

      Ops.lintPageJson = lintPageJson;
    }
  };
  if (global.DynKernel) global.DynKernel.register(__plugin);
  else (global.__DYN_KERNEL_PENDING__ = global.__DYN_KERNEL_PENDING__ || []).push(__plugin);
})(window);

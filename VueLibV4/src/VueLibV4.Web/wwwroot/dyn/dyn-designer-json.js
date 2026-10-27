/* dyn-designer-json.js 设计器落库 JSON 清洗
 * DynDesignerOps.cleanJson(obj)     递归删除 {}、[]、null、""、__uid/__dynId
 * DynDesignerOps.getCleanJson(scope) 取 pageJson 的清洗结果（保存/预览共用）
 */
(function (global) {
  'use strict';
  var __plugin = {
    name: 'designer-json', stage: 'designer', requires: ['DynDesignerOps'],
    setup: function () {
      'use strict';
      const Ops = global.DynDesignerOps;

      function cleanJson(obj) {
        if (obj === null || obj === undefined) return undefined;
        if (Array.isArray(obj)) {
          const arr = obj.map(cleanJson).filter(function (v) { return v !== undefined; });
          return arr.length ? arr : undefined;
        }
        if (typeof obj === 'object') {
          const result = {};
          for (const key in obj) {
            if (key === '__uid' || key === '__dynId') continue;
            const v = cleanJson(obj[key]);
            if (v !== undefined) result[key] = v;
          }
          return Object.keys(result).length ? result : undefined;
        }
        if (typeof obj === 'string' && obj === '') return undefined;
        return obj;
      }

      Ops.cleanJson = cleanJson;
      Ops.getCleanJson = function (scope) { return cleanJson(scope && scope.pageJson); };
    }
  };
  if (global.DynKernel) global.DynKernel.register(__plugin);
  else (global.__DYN_KERNEL_PENDING__ = global.__DYN_KERNEL_PENDING__ || []).push(__plugin);
})(window);

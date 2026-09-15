/* ============================================================
 * VueLibV3  dyn-validator.js  动态校验器
 * 支持规则：required / email / phone / pattern / min / max / length / custom(脚本)
 * validators 配置示例：
 *   [{ "type": "required", "message": "必填" },
 *    { "type": "pattern", "pattern": "^1[3-9]\\d{9}$", "message": "手机号格式不正确" },
 *    { "type": "custom", "script": "return v && v.length >= 2;", "message": "至少2位" }]
 * ============================================================ */
(function (global) {
  'use strict';

  var RULES = {
    required: function (v) { return v !== null && v !== undefined && v !== '' && !(Array.isArray(v) && v.length === 0); },
    email: function (v) { return !v || /^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(v); },
    phone: function (v) { return !v || /^1[3-9]\d{9}$/.test(v); },
    pattern: function (v, opt) { return !v || new RegExp(opt.pattern).test(v); },
    min: function (v, opt) { return v === '' || v === null || Number(v) >= Number(opt.min); },
    max: function (v, opt) { return v === '' || v === null || Number(v) <= Number(opt.max); },
    length: function (v, opt) { return !v || String(v).length >= Number(opt.length); },
    custom: function (v, opt, form) {
      if (!opt.script) return true;
      try {
        var fn = new Function('v', 'form', 'return (' + opt.script + ');');
        return !!fn(v, form);
      } catch (e) { console.error('[DynValidator] custom 脚本异常', e); return false; }
    }
  };

  /** 校验单个值，返回错误信息数组 */
  function validate(validators, value, form) {
    var errors = [];
    (validators || []).forEach(function (item) {
      var rule = RULES[item.type];
      if (!rule) return;
      if (!rule(value, item, form)) {
        errors.push({ type: item.type, message: item.message || '校验未通过' });
      }
    });
    return errors;
  }

  /** 转换为 ElementPlus el-form rules 格式 */
  function toRules(validators) {
    var rules = [];
    (validators || []).forEach(function (item) {
      if (item.type === 'required') {
        rules.push({ required: true, message: item.message || '必填', trigger: item.trigger || 'blur' });
      } else if (item.type === 'pattern') {
        rules.push({ pattern: new RegExp(item.pattern), message: item.message || '格式不正确', trigger: 'blur' });
      } else if (item.type === 'custom') {
        rules.push({
          validator: function (rule, value, callback) {
            var ok = RULES.custom(value, item, rule.form || undefined);
            ok ? callback() : callback(new Error(item.message || '校验未通过'));
          },
          trigger: 'blur'
        });
      } else {
        rules.push({
          validator: function (rule, value, callback) {
            var ok = RULES[item.type] ? RULES[item.type](value, item) : true;
            ok ? callback() : callback(new Error(item.message || '校验未通过'));
          },
          trigger: 'blur'
        });
      }
    });
    return rules;
  }

  global.DynValidator = {
    rules: RULES,
    validate: validate,
    toRules: toRules
  };
})(window);

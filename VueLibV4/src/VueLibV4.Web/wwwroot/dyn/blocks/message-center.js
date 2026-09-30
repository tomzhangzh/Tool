/**
 * message-center.js — 积木消息中心（Command / Event 分离）
 * ------------------------------------------------------------
 * 每个三屏模板（TreeRoot）创建自己独享的 msgCenter 实例，注入到各个积木 Block。
 * 规则：
 *  - Command（命令）：定向发送给指定 Block，异步返回 Promise，可捕获成功/失败。
 *    同一 blockId 允许注册多个接收者（同页多个同类积木），命令扇出给全部接收者；
 *    仅一个接收者时直接返回其 Promise，多个时返回 Promise.all，向后兼容。
 *    需要定向区分的积木请使用不同 blockId（如 list / childList）。
 *  - Event（事件）：广播发布，一对多，只读通知，不要求任何 Block 响应。
 * 积木之间不直接通信，一律通过 msgCenter 中转；积木只处理发给自己的命令、只发布自己的事件。
 * 生命周期：模板销毁时调用 destroy()，一次性清空全部命令接收者与事件订阅，杜绝内存泄漏。
 *
 * 用法（模板 TreeRoot 内）：
 *   var msg = createMessageCenter();
 *   msg.registerBlock('list', listBlock.handleCommand);
 *   msg.subscribeEvent('filter.changed', fn);
 *   msg.sendCommand('detail', {cmd:'newForm', preFill:{...}});
 *   msg.emitEvent('detail.saved', {action:'update', row:...});
 *   msg.destroy();
 */
(function (global) {
    function createMessageCenter() {
        var destroyed = false;
        var eventSubs = new Map();    // eventName -> handler[]
        var cmdReceivers = new Map(); // blockId -> handler[]（同名多接收者，支持同页多个同类积木）

        return {
            // 【注册】积木注册自己，声明可接收命令；同名 blockId 追加而非覆盖，返回注销函数
            registerBlock: function (blockId, handler) {
                var list = cmdReceivers.get(blockId);
                if (!list) { list = []; cmdReceivers.set(blockId, list); }
                list.push(handler);
                var removed = false;
                return function () {
                    if (removed) return;
                    removed = true;
                    var cur = cmdReceivers.get(blockId);
                    if (!cur) return;
                    var i = cur.indexOf(handler);
                    if (i > -1) cur.splice(i, 1);
                    if (!cur.length) cmdReceivers.delete(blockId);
                };
            },

            // 【定向命令】发送给指定 Block，返回 Promise（可捕获成功/失败）
            // 契约：sendCommand(blockId, {cmd:'loadData', ...payload})；
            //      下发时拆分为 handler(cmdName, commandObj)，供 Block.handleCommand(cmd, payload) 解析。
            //      同名多接收者时扇出全部，返回 Promise.all。
            sendCommand: function (blockId, cmd) {
                if (destroyed) return Promise.reject(new Error('messageCenter destroyed'));
                var list = cmdReceivers.get(blockId);
                if (!list || !list.length) return Promise.reject(new Error('积木未注册: ' + blockId));
                var cmdName = cmd && cmd.cmd ? cmd.cmd : cmd;
                var results = list.slice().map(function (h) {
                    try { return Promise.resolve(h(cmdName, cmd)); }
                    catch (e) { return Promise.reject(e); }
                });
                return results.length === 1 ? results[0] : Promise.all(results);
            },

            // 【广播事件】订阅；返回取消订阅函数
            subscribeEvent: function (eventName, handler) {
                if (!eventSubs.has(eventName)) eventSubs.set(eventName, []);
                eventSubs.get(eventName).push(handler);
                return function () {
                    var l = eventSubs.get(eventName);
                    if (!l) return;
                    var i = l.indexOf(handler);
                    if (i > -1) l.splice(i, 1);
                };
            },

            // 【广播事件】发布
            emitEvent: function (eventName, payload) {
                if (destroyed) return;
                var l = eventSubs.get(eventName);
                if (!l) return;
                l.slice().forEach(function (h) {
                    try { h(payload); } catch (e) { console.error('[msg:' + eventName + ']', e); }
                });
            },

            // 【销毁】页面模板销毁时调用，一次性清空所有订阅/接收者
            destroy: function () {
                destroyed = true;
                eventSubs.clear();
                cmdReceivers.clear();
            }
        };
    }

    global.createMessageCenter = createMessageCenter;
})(window);

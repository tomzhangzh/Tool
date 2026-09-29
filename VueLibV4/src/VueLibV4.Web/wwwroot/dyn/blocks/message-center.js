/**
 * message-center.js — 积木消息中心（Command / Event 分离）
 * ------------------------------------------------------------
 * 每个三屏模板（TreeRoot）创建自己独享的 msgCenter 实例，注入到各个积木 Block。
 * 规则：
 *  - Command（命令）：定向发送给指定 Block，一对一，异步返回 Promise，可捕获成功/失败。
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
        var eventSubs = new Map();   // eventName -> handler[]
        var cmdReceivers = new Map(); // blockId -> handleCommand(payload)

        return {
            // 【注册】积木注册自己，声明可接收命令；返回注销函数
            registerBlock: function (blockId, handler) {
                cmdReceivers.set(blockId, handler);
                return function () { cmdReceivers.delete(blockId); };
            },

            // 【定向命令】发送给指定 Block，返回 Promise（可捕获成功/失败）
            // 契约：sendCommand(blockId, {cmd:'loadData', ...payload})；
            //      下发时拆分为 handler(cmdName, commandObj)，供 Block.handleCommand(cmd, payload) 解析。
            sendCommand: function (blockId, cmd) {
                if (destroyed) return Promise.reject(new Error('messageCenter destroyed'));
                var h = cmdReceivers.get(blockId);
                if (!h) return Promise.reject(new Error('积木未注册: ' + blockId));
                try { return Promise.resolve(h(cmd && cmd.cmd ? cmd.cmd : cmd, cmd)); }
                catch (e) { return Promise.reject(e); }
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

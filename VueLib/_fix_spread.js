const fs = require('fs');
const p = 'E:/Tom/Tool/VueLib/src/VueLib.Web/wwwroot/js/designer.js';
let c = fs.readFileSync(p, 'utf8');

const old = `                const sortable = Sortable.create(el, {
                    animation: options.animation || 150,
                    group: options.group || 'lc-designer-group',
                    ghostClass: options.ghostClass || 'lc-ghost',
                    draggable: options.draggable || '>.lc-node',
                    forceFallback: true,
                    fallbackClass: 'lc-dragging',
                    onEnd(evt) {
                        // 内部排序：同步数组顺序
                        if (evt.from === evt.to && evt.oldIndex !== evt.newIndex && evt.oldIndex != null) {
                            const item = list.splice(evt.oldIndex, 1)[0];
                            list.splice(evt.newIndex, 0, item);
                        }
                        if (typeof options.onEnd === 'function') options.onEnd(evt);
                    },
                    onAdd(evt) {
                        // 从其他容器拖入：由容器的 onContainerDragAdd 处理数组插入
                        if (typeof options.onAdd === 'function') options.onAdd(evt);
                    },
                    onRemove(evt) {
                        // 从当前容器拖出：由目标容器的 onAdd 处理
                        if (typeof options.onRemove === 'function') options.onRemove(evt);
                    }
                });`;

const neu = `                const sortable = Sortable.create(el, {
                    animation: 150,
                    group: 'lc-designer-group',
                    ghostClass: 'lc-ghost',
                    draggable: '>.lc-node',
                    forceFallback: true,
                    fallbackClass: 'lc-dragging',
                    ...options,
                    onEnd(evt) {
                        // 内部排序：同步数组顺序
                        if (evt.from === evt.to && evt.oldIndex !== evt.newIndex && evt.oldIndex != null) {
                            const item = list.splice(evt.oldIndex, 1)[0];
                            list.splice(evt.newIndex, 0, item);
                        }
                        if (typeof options.onEnd === 'function') options.onEnd(evt);
                    }
                });`;

if (c.indexOf(neu) >= 0) {
    console.log('already updated');
} else if (c.indexOf(old) >= 0) {
    c = c.replace(old, neu);
    fs.writeFileSync(p, c, 'utf8');
    console.log('updated: spread options to pass through all callbacks');
} else {
    console.log('FAIL: pattern not found');
    const i = c.indexOf('const sortable = Sortable.create');
    if (i >= 0) console.log('context:', JSON.stringify(c.substring(i, i+600)));
}

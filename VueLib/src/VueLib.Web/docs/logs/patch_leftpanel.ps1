$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$e = New-Object System.Text.UTF8Encoding($false)
$f = "E:\Tom\Tool\VueLib\src\VueLib.Web\Views\Designer\_LeftPanel.cshtml"
$t = [System.IO.File]::ReadAllText($f, $e)
$N = "`n"

# ---- 1) el-tree 模板：图标 + allow-drop + node-drop ----
$old = '                    <el-tree ref="treeRef"' + $N +
'                             :key="treeVersion"' + $N +
'                             :data="treeData"' + $N +
'                             :props="{ children: ''childrenctrls'', label: ''component'' }"' + $N +
'                             draggable' + $N +
'                             default-expand-all' + $N +
'                             :filter-node-method="(value, data) => !value || data.component.includes(value)"' + $N +
'                             @@node-click="selectFromTree">' + $N +
'                    </el-tree>'
$new = '                    <el-tree ref="treeRef"' + $N +
'                             :key="treeVersion"' + $N +
'                             :data="treeData"' + $N +
'                             :props="{ children: ''childrenctrls'', label: ''component'' }"' + $N +
'                             draggable' + $N +
'                             default-expand-all' + $N +
'                             :filter-node-method="(value, data) => !value || data.component.includes(value)"' + $N +
'                             :allow-drop="treeAllowDrop"' + $N +
'                             @@node-click="selectFromTree"' + $N +
'                             @@node-drop="onTreeDrop">' + $N +
'                        <template #default="{ data }">' + $N +
'                            <span class="lc-tree-item">' + $N +
'                                <span class="lc-tree-icon">{{ treeIcon(data) }}</span>' + $N +
'                                <span class="lc-tree-label">{{ treeLabel(data) }}</span>' + $N +
'                                <span v-if="data.__compositeInternal" class="lc-tree-badge" title="组合内部固定节点">锁</span>' + $N +
'                            </span>' + $N +
'                        </template>' + $N +
'                    </el-tree>'
if ($t.Contains($old)) { $t = $t.Replace($old, $new); Write-Output "1) tree 模板已更新" } else { Write-Output "1) WARN 未匹配" }

# ---- 2) setup 方法 ----
$anchor = '                // 组件树点击：组合内部节点 → 选中所属组合根；其余 → 选中真实节点' + $N +
'                function selectFromTree(data) {'
$methods = '                // ===== 组件树图标/名称（优先 META，其次 options 配置，兜底占位）=====' + $N +
'                function treeIcon(data) {' + $N +
'                    var name = (data && data.component) || '''';' + $N +
'                    var meta = (window.dynCom && dynCom.get) ? dynCom.get(name) : null;' + $N +
'                    if (meta && meta.icon) return meta.icon;' + $N +
'                    var cc = compositeComponents[name];' + $N +
'                    if (cc && cc.icon) return cc.icon;' + $N +
'                    if (data && data.options && data.options.icon) return data.options.icon;' + $N +
'                    return ''📦'';' + $N +
'                }' + $N +
'                function treeLabel(data) {' + $N +
'                    var name = (data && data.component) || '''';' + $N +
'                    var lab = (data && data.options && data.options.labeloptions && data.options.labeloptions.label);' + $N +
'                    if (lab) return lab;' + $N +
'                    var meta = (window.dynCom && dynCom.get) ? dynCom.get(name) : null;' + $N +
'                    return (meta && meta.label) ? (meta.label + '' · '' + name) : name;' + $N +
'                }' + $N +
$N +
'                // 在真实 configObj 中查找 target 所属 childrenctrls 数组' + $N +
'                function findRealArray(root, target) {' + $N +
'                    if (!root || typeof root !== ''object'') return null;' + $N +
'                    var arr = root.childrenctrls;' + $N +
'                    if (Array.isArray(arr)) {' + $N +
'                        for (var i = 0; i < arr.length; i++) {' + $N +
'                            if (arr[i] === target) return { arr: arr, index: i };' + $N +
'                            var inner = findRealArray(arr[i], target);' + $N +
'                            if (inner) return inner;' + $N +
'                        }' + $N +
'                    }' + $N +
'                    return null;' + $N +
'                }' + $N +
$N +
'                // el-tree 拖拽落点：把新顺序写回 configObj → 画布随之更新' + $N +
'                function onTreeDrop(draggingNode, dropNode, dropType) {' + $N +
'                    if (!draggingNode || !dropNode) return;' + $N +
'                    var dragSrc = draggingNode.data.__src || draggingNode.data;' + $N +
'                    if (draggingNode.data.__compositeInternal) { S.treeVersion.value++; return; }' + $N +
'                    var holder = findRealArray(S.configObj, dragSrc);' + $N +
'                    if (!holder) return;' + $N +
'                    var targetNode = dropNode.data.__src || dropNode.data;' + $N +
'                    var targetArr, insertIdx;' + $N +
'                    if (dropType === ''inner'') {' + $N +
'                        if (!Array.isArray(targetNode.childrenctrls)) targetNode.childrenctrls = [];' + $N +
'                        targetArr = targetNode.childrenctrls;' + $N +
'                        insertIdx = targetArr.length;' + $N +
'                    } else {' + $N +
'                        var th = findRealArray(S.configObj, targetNode);' + $N +
'                        if (!th) return;' + $N +
'                        targetArr = th.arr;' + $N +
'                        insertIdx = th.index + (dropType === ''after'' ? 1 : 0);' + $N +
'                    }' + $N +
'                    holder.arr.splice(holder.index, 1);' + $N +
'                    if (targetArr === holder.arr && insertIdx > holder.index) insertIdx--;' + $N +
'                    targetArr.splice(Math.max(0, insertIdx), 0, dragSrc);' + $N +
'                    S.treeVersion.value++;' + $N +
'                    window.dispatchEvent(new CustomEvent(''lc-tree-refresh''));' + $N +
'                    if (window.dyn && dyn.eventBus) dyn.eventBus.emit(''treechange'', { target: dragSrc });' + $N +
'                }' + $N +
$N +
'                // 禁止拖拽组合内部固定节点 / 拖入组合内部固定节点' + $N +
'                function treeAllowDrop(draggingNode, dropNode, type) {' + $N +
'                    if (draggingNode.data && draggingNode.data.__compositeInternal) return false;' + $N +
'                    if (type === ''inner'' && dropNode.data && dropNode.data.__compositeInternal) return false;' + $N +
'                    return true;' + $N +
'                }' + $N +
$N +
'                // 组件树点击：组合内部节点 → 选中所属组合根；其余 → 选中真实节点' + $N +
'                function selectFromTree(data) {'
if ($t.Contains($anchor)) { $t = $t.Replace($anchor, $methods); Write-Output "2) 树方法已插入" } else { Write-Output "2) WARN 方法锚点未匹配" }

# ---- 3) return 暴露 ----
$oldRet = '                    onPaletteDragStart: onPaletteDragStart, onPaletteDragEnd: onPaletteDragEnd,' + $N +
'                    selectFromTree: selectFromTree,'
$newRet = '                    onPaletteDragStart: onPaletteDragStart, onPaletteDragEnd: onPaletteDragEnd,' + $N +
'                    selectFromTree: selectFromTree,' + $N +
'                    onTreeDrop: onTreeDrop, treeAllowDrop: treeAllowDrop,' + $N +
'                    treeIcon: treeIcon, treeLabel: treeLabel,'
if ($t.Contains($oldRet)) { $t = $t.Replace($oldRet, $newRet); Write-Output "3) return 已更新" } else { Write-Output "3) WARN return 未匹配" }

[System.IO.File]::WriteAllText($f, $t, $e)
Write-Output "---- _LeftPanel.cshtml 完成 ----"

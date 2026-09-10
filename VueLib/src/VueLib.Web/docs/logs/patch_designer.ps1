$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$e = New-Object System.Text.UTF8Encoding($false)
$f = "E:\Tom\Tool\VueLib\src\VueLib.Web\wwwroot\js\designer.js"
$t = [System.IO.File]::ReadAllText($f, $e)
$N = "`n"
$n = 0

# ---- 1) lcProvider 暴露 moveUp/moveDown/copyCurrent ----
$old1 = '                dragGroup: DRAG_GROUP,' + $N + '                deleteCurrent' + $N + '            };'
$new1 = '                dragGroup: DRAG_GROUP,' + $N + '                deleteCurrent,' + $N + '                moveUp, moveDown, copyCurrent' + $N + '            };'
if ($t.Contains($old1)) { $t = $t.Replace($old1, $new1); $n++ } else { Write-Output "1) WARN lcProvider" }

# ---- 2) 工具条模板：选中时显示 上移/下移/复制/删除 + tbNearTop 翻转类 ----
$old2 = '                <div v-if="isDesign" class="lc-node-toolbar" v-on:mousedown.stop.prevent="onToolbarDown" v-on:click.stop>' + $N +
'                    <span class="lc-node-tb-drag" title="拖拽移动">⋮⋮</span>' + $N +
'                    <span class="lc-node-tb-label">{{ componentLabel }}</span>' + $N +
'                    <span class="lc-node-tb-del" title="删除组件" v-on:click.stop="removeNode">✕</span>' + $N +
'                </div>'
$new2 = '                <div v-if="isDesign" class="lc-node-toolbar" :class="{ ''lc-tb-below'': tbNearTop }" v-on:mousedown.stop.prevent="onToolbarDown" v-on:click.stop>' + $N +
'                    <span class="lc-node-tb-drag" title="拖拽移动">⋮⋮</span>' + $N +
'                    <span class="lc-node-tb-label">{{ componentLabel }}</span>' + $N +
'                    <span v-if="isSelected" class="lc-node-tb-ops">' + $N +
'                        <span class="lc-node-tb-btn" title="上移" v-on:click.stop="moveNode(-1)">↑</span>' + $N +
'                        <span class="lc-node-tb-btn" title="下移" v-on:click.stop="moveNode(1)">↓</span>' + $N +
'                        <span class="lc-node-tb-btn" title="复制" v-on:click.stop="copyNode">⧉</span>' + $N +
'                        <span class="lc-node-tb-btn lc-node-tb-del" title="删除组件" v-on:click.stop="removeNode">✕</span>' + $N +
'                    </span>' + $N +
'                </div>'
if ($t.Contains($old2)) { $t = $t.Replace($old2, $new2); $n++ } else { Write-Output "2) WARN 工具条" }

# ---- 3) 组件加 data()（tbNearTop 标志） ----
$old3 = '        provide() {' + $N + '            return {' + $N + '                lcLocked: computed(() => this.isLocked),'
$new3 = '        data() { return { tbNearTop: false }; },' + $N + '        provide() {' + $N + '            return {' + $N + '                lcLocked: computed(() => this.isLocked),'
if ($t.Contains($old3)) { $t = $t.Replace($old3, $new3); $n++ } else { Write-Output "3) WARN data()" }

# ---- 4) 生命周期钩子（mounted/updated/beforeUnmount，放在 methods: 前） ----
$old4 = '        methods: {' + $N + '            removeNode() {'
$new4 = '        mounted() { this._observeTb(); },' + $N + '        updated() { this._observeTb(); },' + $N + '        beforeUnmount() {' + $N + '            if (this._tbScrollEl) { this._tbScrollEl.removeEventListener(''scroll'', this._tbHandler, true); this._tbScrollEl = null; }' + $N + '        },' + $N + '        methods: {' + $N + '            // 工具条贴近画布顶部时翻转到节点下方，避免显示不全' + $N + '            _observeTb() {' + $N + '                var el = this.$el; if (!el) return;' + $N + '                var scrollEl = el.closest(''.canvas-scroll'') || el.parentElement;' + $N + '                if (this._tbScrollEl !== scrollEl) {' + $N + '                    if (this._tbScrollEl) this._tbScrollEl.removeEventListener(''scroll'', this._tbHandler, true);' + $N + '                    this._tbScrollEl = scrollEl;' + $N + '                    if (scrollEl) scrollEl.addEventListener(''scroll'', this._tbHandler, true);' + $N + '                }' + $N + '                this._tbUpdate();' + $N + '            },' + $N + '            _tbUpdate() {' + $N + '                var el = this.$el; if (!el) return;' + $N + '                var scrollEl = this._tbScrollEl || (el.closest(''.canvas-scroll'') || el.parentElement);' + $N + '                if (!scrollEl) return;' + $N + '                var sr = scrollEl.getBoundingClientRect();' + $N + '                var er = el.getBoundingClientRect();' + $N + '                var near = (er.top - sr.top) < 26;' + $N + '                if (near !== this.tbNearTop) this.tbNearTop = near;' + $N + '            },' + $N + '            _tbHandler() { this._tbUpdate(); },' + $N + '            removeNode() {'
if ($t.Contains($old4)) { $t = $t.Replace($old4, $new4); $n++ } else { Write-Output "4) WARN hooks" }

# ---- 5) 上移/下移/复制 方法（在 removeNode 后） ----
$old5 = '            onToolbarDown() { /* 拖拽由容器 Sortable 处理；手柄仅提供视觉与选中提示 */ },'
$new5 = '            moveNode(dir) {' + $N + '                const d = this.lcDesigner; if (!d) return;' + $N + '                d.setCurrentCom(this.jsonconfig);' + $N + '                if (dir < 0 && typeof d.moveUp === ''function'') d.moveUp();' + $N + '                else if (dir > 0 && typeof d.moveDown === ''function'') d.moveDown();' + $N + '            },' + $N + '            copyNode() {' + $N + '                const d = this.lcDesigner; if (!d) return;' + $N + '                d.setCurrentCom(this.jsonconfig);' + $N + '                if (typeof d.copyCurrent === ''function'') d.copyCurrent();' + $N + '            },' + $N + '            onToolbarDown() { /* 拖拽由容器 Sortable 处理；手柄仅提供视觉与选中提示 */ },'
if ($t.Contains($old5)) { $t = $t.Replace($old5, $new5); $n++ } else { Write-Output "5) WARN move/copy" }

[System.IO.File]::WriteAllText($f, $t, $e)
Write-Output "designer.js 补丁: $n/5"

/**
 * 把 npm 包中的 UMD 产物拷贝到 wwwroot/lib，供 cshtml 直接引用。
 * 运行：npm run build:libs
 */
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const nodeModules = path.join(root, 'node_modules');
const outRoot = path.resolve(root, '../src/VueLib.Web/wwwroot/lib');

const jobs = [
  { from: 'vue/dist/vue.global.prod.js', to: 'vue/vue.global.prod.js' },
  { from: 'vue-router/dist/vue-router.global.prod.js', to: 'vue-router/vue-router.global.prod.js' },
  { from: 'lodash/lodash.min.js', to: 'lodash/lodash.min.js' },
  { from: 'axios/dist/axios.min.js', to: 'axios/axios.min.js' },
  { from: 'element-plus/dist/index.full.min.js', to: 'element-plus/index.full.min.js' },
  { from: 'element-plus/dist/index.css', to: 'element-plus/index.css' },
  { from: 'sortablejs/Sortable.min.js', to: 'sortable/Sortable.min.js' }
];

let count = 0;
for (const j of jobs) {
  const src = path.join(nodeModules, j.from);
  const dst = path.join(outRoot, j.to);
  if (!fs.existsSync(src)) {
    console.error('[copy-libs] 缺失: ' + j.from + '（请先 npm install）');
    continue;
  }
  fs.mkdirSync(path.dirname(dst), { recursive: true });
  fs.copyFileSync(src, dst);
  console.log('[copy-libs] ✓ ' + j.to);
  count++;
}
console.log('[copy-libs] 完成 ' + count + '/' + jobs.length + ' 个文件 → ' + outRoot);

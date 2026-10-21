// 自包含打包：React19 + react-dom/client + 全量 pro-ui → 单个 UMD iife
import { build } from 'esbuild';
import path from 'path';
import fs from 'fs';

const outDir = 'E:/Tom/Tool/VueLibV4/src/VueLibV4.Web/wwwroot/lib';

await build({
  entryPoints: ['E:/Tom/Tool/VueLibV4/build-pro-ui/entry.js'],
  bundle: true,
  format: 'iife',
  globalName: 'ProUI',
  outfile: path.join(outDir, 'pro-ui.umd.js'),
  minify: true,
  target: ['es2019'],
  legalComments: 'none',
  logLevel: 'info',
});

// CSS 已由 tailwind CLI 生成，这里不重复
console.log('打包完成：', path.join(outDir, 'pro-ui.umd.js'));
console.log('大小：', Math.round(fs.statSync(path.join(outDir, 'pro-ui.umd.js')).size / 1024), 'KB');

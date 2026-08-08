// 简单的本地 HTTPS 服务器脚本
// 使用 Node.js 内置的 http-server 或 npx serve

const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3001;
const DIST_DIR = path.join(__dirname, 'dist');

const MIME_TYPES = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf'
};

const server = http.createServer((req, res) => {
  let filePath = path.join(DIST_DIR, req.url === '/' ? 'index.html' : req.url);
  const ext = path.extname(filePath);
  const contentType = MIME_TYPES[ext] || 'application/octet-stream';

  fs.readFile(filePath, (err, content) => {
    if (err) {
      // SPA fallback：找不到文件返回 index.html
      if (!ext) {
        fs.readFile(path.join(DIST_DIR, 'index.html'), (err2, content2) => {
          if (err2) {
            res.writeHead(404);
            res.end('Not found');
          } else {
            res.writeHead(200, { 'Content-Type': 'text/html' });
            res.end(content2);
          }
        });
      } else {
        res.writeHead(404);
        res.end('Not found');
      }
    } else {
      // 添加 Service Worker 相关 headers
      const headers = {
        'Content-Type': contentType
      };
      
      // Service Worker 必须有正确的 scope
      if (filePath.endsWith('service-worker.js')) {
        headers['Service-Worker-Allowed'] = '/';
      }
      
      res.writeHead(200, headers);
      res.end(content);
    }
  });
});

server.listen(PORT, () => {
  console.log(`✅ 本地服务器已启动: http://localhost:${PORT}`);
  console.log(`📱 手机测试: http://<你的IP>:${PORT}`);
  console.log(``);
  console.log(`PWA 测试步骤:`);
  console.log(`1. 浏览器打开 http://localhost:${PORT}`);
  console.log(`2. F12 → Application → Service Workers 查看 SW`);
  console.log(`3. F12 → Application → Manifest 查看配置`);
  console.log(`4. 地址栏输入 chrome://apps 查看安装`);
  console.log(`   或地址栏应出现 安装图标`);
});

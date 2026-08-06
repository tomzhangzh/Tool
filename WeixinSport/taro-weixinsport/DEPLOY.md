# 运动小达人 (Taro 版) 部署指南

本文档介绍如何将改造后的 Taro 项目同时部署为**微信小程序**和**H5 应用**。

## 1. 环境准备

确保你的电脑已安装：
- [ ] Node.js (推荐 v18 或更高版本)
- [ ] NPM (Node 包管理器，随 Node 一起安装)
- [ ] 微信开发者工具 (用于小程序调试和上传)

## 2. 安装依赖

进入 `taro-weixinsport` 目录，运行 `npm install` 安装所有依赖。

```bash
cd taro-weixinsport
npm install
```

## 3. 配置云开发环境

在 `src/utils/cloud.js` 中，将 `ENV_ID` 常量替换为你自己的**微信云开发环境 ID**。

```javascript
// src/utils/cloud.js
const ENV_ID = '你的云开发环境ID'; 
```

## 4. 运行与调试

### 4.1 H5 模式 (开发预览)

运行后，将在本地启动一个 HTTP 服务，可在浏览器直接访问。

```bash
npm run dev:h5
```
- 打开浏览器访问 `http://localhost:10086` (默认端口)。
- 此时调用的是真实的云开发数据库和云函数。

### 4.2 小程序模式 (开发预览)

运行后，会在 `dist` 目录生成小程序代码，用微信开发者工具打开即可预览。

```bash
npm run dev:weapp
```
- 打开微信开发者工具，导入 `taro-weixinsport/dist` 目录。

## 5. 生产环境部署

### 5.1 部署 H5 (目标：微信云开发静态网站托管)

1.  **打包 H5 产物**
    ```bash
    npm run build:h5
    ```
    产物将生成在 `dist` 目录下。

2.  **上传到云开发静态托管**
    - 登录 [微信云开发控制台](https://cloud.weixin.qq.com/)。
    - 选择你的环境 -> 静态网站托管 -> 开启。
    - 将 `taro-weixinsport/dist` 目录下的所有文件上传到托管根目录。

3.  **访问 H5 应用**
    - 云开发会提供一个默认域名（如 `env-xxxxx.tcloudbaseapp.com`）。
    - 访问该域名即可使用 H5 版应用。该域名已自动加入云开发安全白名单，可直接调用云函数。

### 5.2 部署小程序

1.  **打包小程序产物**
    ```bash
    npm run build:weapp
    ```
    产物将生成在 `dist` 目录下。

2.  **上传到微信后台**
    - 打开微信开发者工具 -> 导入项目，目录选择 `taro-weixinsport/dist`。
    - 在开发者工具中点击“上传”按钮，填写版本号后提交。
    - 登录微信公众平台 -> 版本管理 -> 将开发版提交审核。

## 6. H5 端微信登录 (重要)

H5 应用要获取用户的微信身份（openid），需要配置微信网页授权：

1.  **获取公众号 AppID 和 AppSecret**
    - 登录 [微信公众平台](https://mp.weixin.qq.com/)。
    - 基本配置 -> 开发者ID -> 记录下 AppID 和 AppSecret。

2.  **配置授权域名**
    - 微信公众平台 -> 基本配置 -> 网页授权域名 -> 填入你 H5 的访问域名（云开发默认域名或自定义域名）。

3.  **修改 H5 登录逻辑**
    - 打开 `src/utils/auth.js`。
    - 将 `redirectToWxAuth` 函数中的 `APPID` 常量改为你的公众号 AppID。

4.  **云函数增加换取 openid 逻辑**
    - 在你的 `login` 云函数中，需要增加一个 `h5Login` action，用于接收 H5 传来的 `code`，并调用微信的 `sns/jscode2session` 接口换取 `openid`。
    - 因为 `sns/jscode2session` 接口需要 AppSecret，必须在云函数（后端）调用，不能在前端调用。

## 7. 常见问题

**Q: H5 页面打开白屏？**
A: 检查 `index.html` 是否正确引入了 `tcb-js-sdk`。打开浏览器开发者工具，查看 Console 是否有 `云开发未初始化` 的错误。

**Q: H5 调用云函数报 `权限校验失败`？**
A: 登录微信云开发控制台 -> 安全配置 -> WEB端安全域名 -> 添加你的 H5 域名。

**Q: H5 登录卡在授权页面？**
A: 检查公众号后台是否配置了“网页授权域名”。确保 `auth.js` 中拼接的授权 URL 正确，且 `redirect_uri` 与后台配置一致。

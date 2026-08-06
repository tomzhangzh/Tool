// src/app.js
import './app.scss';
import './styles/common.scss';
import { initCloud } from './utils/cloud';

// 初始化云开发（异步，H5 环境需要等待匿名登录完成）
const cloudReadyPromise = initCloud();

// 导出等待云开发就绪的方法
export const waitForCloudReady = () => cloudReadyPromise;

function App({ children }) {
  return children
}

export default App

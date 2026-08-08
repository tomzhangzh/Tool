// src/utils/cloud.js
// 云开发环境初始化与适配层
import Taro from '@tarojs/taro';
import cloudbase from '@cloudbase/js-sdk';

const ENV_ID = 'cloud1-d9g0cl0c6e5006db7'; 
const LOCAL_USER_ID_KEY = 'weixinsport_local_uid';
const INIT_VERSION_KEY = 'weixinsport_init_version';
const FORCE_RESET_FLAG = 'weixinsport_force_reset';

let cloudInstance = null;
let isAnonymousLogin = false;
let initPromise = null;

// 判断是否为微信浏览器
const isWeChatBrowser = () => {
  if (typeof navigator === 'undefined') return false;
  return /MicroMessenger/i.test(navigator.userAgent);
};

// 生成UUID v4
const generateUUID = () => {
  try {
    if (crypto && crypto.randomUUID) {
      return crypto.randomUUID();
    }
  } catch (e) {}
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};

// 获取或创建本地用户ID
const getOrCreateLocalUserId = () => {
  try {
    let uid = localStorage.getItem(LOCAL_USER_ID_KEY);
    if (!uid) {
      uid = generateUUID();
      localStorage.setItem(LOCAL_USER_ID_KEY, uid);
      console.log('[localUserId] 生成新的本地用户ID:', uid);
    }
    return uid;
  } catch (e) {
    console.warn('[localUserId] 获取失败:', e.message);
    return generateUUID();
  }
};

// 清除本地用户ID
const clearLocalUserId = () => {
  try {
    localStorage.removeItem(LOCAL_USER_ID_KEY);
    console.log('[localUserId] 已清除本地用户ID');
  } catch (e) {
    console.warn('[localUserId] 清除失败:', e.message);
  }
};

// 清除所有 CloudBase 相关的 localStorage
const clearCloudBaseLocalStorage = () => {
  try {
    const prefixes = [
      'credentials_cloud',
      'user_info_cloud',
      'anonymous_uuid',
      'auth_cloud',
      'temp_auth_cloud',
      'lang_cloud',
      'cloudbase_auth',
      'device_id',
    ];
    const keysToRemove = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (prefixes.some(p => key.startsWith(p))) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach(key => {
      try { localStorage.removeItem(key); } catch (e) {}
    });
    console.log('[clearStorage] 清除 CloudBase localStorage:', keysToRemove.length, '项', keysToRemove);
  } catch (e) {
    console.warn('[clearStorage] 清除失败:', e.message);
  }
};

// 带超时的 Promise
const withTimeout = (promise, timeoutMs, label) => {
  return Promise.race([
    promise,
    new Promise((_, reject) => {
      setTimeout(() => reject(new Error(`${label} 超时(${timeoutMs}ms)`)), timeoutMs);
    })
  ]);
};

// 检查是否需要强制重置（URL参数或标记）
const checkForceReset = () => {
  // URL 参数 ?reset=1
  try {
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('reset') === '1') {
      console.log('[forceReset] 检测到 URL 参数 reset=1，执行强制重置');
      return true;
    }
  } catch (e) {}
  
  // 全局标记
  try {
    if (window.__forceReset === true) {
      console.log('[forceReset] 检测到全局重置标记');
      return true;
    }
  } catch (e) {}
  
  return false;
};

// 执行强制重置（清除所有存储 + 重新初始化）
export const forceResetAll = () => {
  console.log('[forceResetAll] 执行强制重置...');
  try {
    // 清除所有相关的 localStorage
    const allKeys = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key.startsWith('cloud') || key.startsWith('weixinsport_') || key.startsWith('credentials') || key.startsWith('user_info') || key.startsWith('auth_') || key.startsWith('anonymous') || key.startsWith('device_id')) {
        allKeys.push(key);
      }
    }
    allKeys.forEach(key => {
      try { localStorage.removeItem(key); } catch (e) {}
    });
    console.log('[forceResetAll] 已清除:', allKeys.length, '项', allKeys);
    
    // 清除 sessionStorage
    try {
      const sessionKeys = [];
      for (let i = 0; i < sessionStorage.length; i++) {
        const key = sessionStorage.key(i);
        if (key.startsWith('cloud') || key.startsWith('weixinsport_') || key.startsWith('credentials') || key.startsWith('user_info')) {
          sessionKeys.push(key);
        }
      }
      sessionKeys.forEach(key => {
        try { sessionStorage.removeItem(key); } catch (e) {}
      });
    } catch (e) {}
    
    // 重置 SDK 实例
    cloudInstance = null;
    isAnonymousLogin = false;
    initPromise = null;
    
    console.log('[forceResetAll] 重置完成，请刷新页面');
    return true;
  } catch (e) {
    console.error('[forceResetAll] 失败:', e.message);
    return false;
  }
};

// 初始化云开发
export const initCloud = async () => {
  if (initPromise) {
    return initPromise;
  }
  
  initPromise = (async () => {
    if (process.env.TARO_ENV !== 'h5') {
      if (Taro.cloud) {
        Taro.cloud.init({ env: ENV_ID });
        cloudInstance = Taro.cloud;
        console.log('云开发初始化成功 (小程序)');
      }
      return;
    }

    if (!cloudbase) {
      console.warn('CloudBase JS SDK 加载失败');
      return;
    }

    const isWX = isWeChatBrowser();
    console.log('[initCloud] 环境:', isWX ? '微信浏览器' : '普通浏览器');

    // 1. 检查 URL 参数或全局重置标记
    const needReset = checkForceReset();
    
    // 2. 检查初始化版本号
    const CURRENT_VERSION = 'v3';
    let versionChanged = false;
    try {
      const savedVersion = localStorage.getItem(INIT_VERSION_KEY);
      if (savedVersion !== CURRENT_VERSION) {
        versionChanged = true;
      }
    } catch (e) {}

    // 3. 如果需要重置或版本变更，彻底清理
    if (needReset || versionChanged) {
      console.log('[initCloud] 执行清理...', { needReset, versionChanged });
      clearCloudBaseLocalStorage();
      // 如果是 URL 参数重置，也清除业务身份
      if (needReset) {
        clearLocalUserId();
        // 从 URL 中移除 reset 参数
        try {
          const url = new URL(window.location.href);
          url.searchParams.delete('reset');
          window.history.replaceState({}, '', url.toString());
        } catch (e) {}
      }
      try {
        localStorage.setItem(INIT_VERSION_KEY, CURRENT_VERSION);
      } catch (e) {}
    }

    // 创建 SDK 实例
    cloudInstance = cloudbase.init({ env: ENV_ID });
    console.log('[initCloud] SDK 实例创建成功');

    // 匿名登录
    try {
      console.log('[initCloud] 开始匿名登录...');
      const auth = cloudInstance.auth();
      const result = await withTimeout(
        auth.signInAnonymously(),
        15000,
        '匿名登录'
      );
      if (result && result.error) {
        throw new Error('匿名登录失败: ' + result.error.message);
      }
      isAnonymousLogin = true;
      const newUser = auth.getCurrentUser();
      console.log('[initCloud] 匿名登录成功:', newUser ? newUser.uid : 'null');
    } catch (err) {
      console.error('[initCloud] 首次匿名登录失败:', err.message);
      
      // 失败后尝试：清存储 → 重建实例 → 重试
      try {
        console.log('[initCloud] 清理存储后重试...');
        clearCloudBaseLocalStorage();
        cloudInstance = cloudbase.init({ env: ENV_ID });
        const auth2 = cloudInstance.auth();
        await withTimeout(auth2.signInAnonymously(), 15000, '重试匿名登录');
        isAnonymousLogin = true;
        console.log('[initCloud] 重试匿名登录成功:', auth2.getCurrentUser()?.uid);
      } catch (retryErr) {
        console.error('[initCloud] 重试也失败:', retryErr.message);
      }
    }
  })();

  return initPromise;
};

// 强制重新初始化（用于鉴权失败后的恢复）
const forceReinitCloudBase = async () => {
  console.log('[forceReinit] 开始强制重新初始化...');
  cloudInstance = null;
  isAnonymousLogin = false;
  initPromise = null;
  clearCloudBaseLocalStorage();
  return initCloud();
};

// 等待初始化完成
export const waitForInit = () => initPromise || Promise.resolve();

// 获取云开发实例
export const getCloud = () => cloudInstance;

// 退出登录（仅清除业务身份）
export const signOutCloudBase = async () => {
  clearLocalUserId();
  console.log('[signOutCloudBase] 业务身份已清除');
};

// 获取当前用户的业务ID
export const getCurrentUid = async () => {
  if (process.env.TARO_ENV === 'h5') {
    return getOrCreateLocalUserId();
  }
  return null;
};

// 确保 H5 环境已登录
const ensureLogin = async () => {
  if (process.env.TARO_ENV !== 'h5') return;
  if (!cloudInstance) {
    await initCloud();
  }
  if (!cloudInstance) {
    throw new Error('云开发未初始化');
  }

  const auth = cloudInstance.auth();
  try {
    const user = auth.getCurrentUser();
    if (user) {
      isAnonymousLogin = true;
      return user;
    }
  } catch (e) {
    console.warn('[ensureLogin] getCurrentUser 异常，重新登录:', e.message);
  }

  try {
    const result = await withTimeout(auth.signInAnonymously(), 15000, 'ensureLogin');
    if (result && result.error) {
      throw new Error('匿名登录失败: ' + result.error.message);
    }
    isAnonymousLogin = true;
    const newUser = auth.getCurrentUser();
    console.log('[ensureLogin] 匿名登录成功:', newUser ? newUser.uid : 'null');
    return newUser;
  } catch (err) {
    console.error('[ensureLogin] 匿名登录失败:', err.message);
    throw new Error('登录失败：' + (err.message || '请检查网络连接'));
  }
};

// 判断是否为鉴权错误
const isAuthError = (err) => {
  const errMsg = JSON.stringify(err);
  return err.error === 'unauthenticated' ||
    err.error_code === 'UNAUTHENTICATED' ||
    errMsg.includes('unauthenticated') ||
    errMsg.includes('credentials not found') ||
    errMsg.includes('credentials');
};

// 调用云函数
export const callFunction = async (name, data = {}) => {
  await waitForInit();

  if (!cloudInstance) {
    await initCloud();
  }

  const cloud = getCloud();
  if (!cloud) {
    throw new Error('云开发未初始化');
  }

  if (process.env.TARO_ENV === 'h5') {
    await ensureLogin();

    const localUid = getOrCreateLocalUserId();
    const requestData = { ...data, _uid: localUid };

    try {
      const res = await cloud.callFunction({ name, data: requestData });
      return res;
    } catch (err) {
      console.error('[callFunction] error:', err);

      if (isAuthError(err)) {
        console.log('[callFunction] 鉴权失效，执行恢复...');
        try {
          await forceReinitCloudBase();
          const retryCloud = getCloud();
          if (retryCloud) {
            return await retryCloud.callFunction({ name, data: { ...data, _uid: localUid } });
          }
        } catch (retryErr) {
          console.error('[callFunction] 恢复重试失败:', retryErr);
        }
        throw new Error('登录状态失效，请刷新页面重试');
      }

      throw err;
    }
  } else {
    return new Promise((resolve, reject) => {
      cloud.callFunction({
        name,
        data,
        success: resolve,
        fail: reject
      });
    });
  }
};

// 上传文件到云存储
export const uploadFile = async (file, cloudPath) => {
  await waitForInit();
  
  if (process.env.TARO_ENV === 'h5') {
    if (!cloudInstance) {
      await initCloud();
    }
    if (!cloudInstance) {
      throw new Error('云开发未初始化');
    }
    await ensureLogin();
    
    const safePath = cloudPath.replace(/[^a-zA-Z0-9_./-]/g, '_');
    
    let fileContent;
    if (file instanceof Blob) {
      fileContent = await file.arrayBuffer();
    } else if (file instanceof ArrayBuffer) {
      fileContent = file;
    } else {
      fileContent = file;
    }
    
    console.log('[uploadFile] cloudPath:', safePath, 'size:', fileContent?.byteLength || file?.size);
    
    const uploadRes = await cloudInstance.uploadFile({
      cloudPath: safePath,
      fileContent
    });
    
    // H5 环境：使用 fileList 参数获取临时链接
    const downloadUrl = await cloudInstance.getTempFileURL({
      fileList: [uploadRes.fileID]
    });
    
    return {
      fileID: uploadRes.fileID,
      url: downloadUrl.fileList?.[0]?.tempFileURL || downloadUrl.fileList?.[0]?.downloadUrl || ''
    };
  } else {
    return new Promise((resolve, reject) => {
      Taro.cloud.uploadFile({
        cloudPath,
        filePath: file,
        success: (res) => {
          resolve({
            fileID: res.fileID,
            url: res.fileID
          });
        },
        fail: reject
      });
    });
  }
};

// 缓存 fileID -> URL 的映射，避免重复请求
const urlCache = new Map();
const URL_CACHE_TTL = 30 * 60 * 1000; // 30 分钟缓存（临时链接有效期 2 小时）

/**
 * 将 fileID 转换为可访问的 URL
 * - 如果已经是 http(s) URL，直接返回
 * - 如果是 fileID（cloud:// 开头），调用 getTempFileURL 获取临时链接
 * - 结果会被缓存 30 分钟
 * @param {string} fileIDOrUrl - fileID 或 URL
 * @returns {Promise<string>} 可访问的 URL
 */
export const resolveFileURL = async (fileIDOrUrl) => {
  if (!fileIDOrUrl || typeof fileIDOrUrl !== 'string' || fileIDOrUrl.trim() === '') {
    return '';
  }
  
  const trimmed = fileIDOrUrl.trim();
  
  // 已经是 http(s) URL，直接返回
  if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
    return trimmed;
  }
  
  // 不是 fileID 格式，直接返回
  if (!trimmed.startsWith('cloud://')) {
    return trimmed;
  }
  
  // 检查缓存
  const cached = urlCache.get(trimmed);
  if (cached && Date.now() - cached.time < URL_CACHE_TTL) {
    return cached.url;
  }
  
  await waitForInit();
  
  try {
    let url;
    
    if (process.env.TARO_ENV === 'h5') {
      if (!cloudInstance) {
        await initCloud();
      }
      if (!cloudInstance) {
        throw new Error('云开发未初始化');
      }
      await ensureLogin();
      
      // H5 环境：使用 fileList 参数
      const result = await cloudInstance.getTempFileURL({
        fileList: [trimmed]
      });
      url = result.fileList?.[0]?.tempFileURL || result.fileList?.[0]?.downloadUrl || '';
    } else {
      // 小程序环境
      url = await new Promise((resolve, reject) => {
        Taro.cloud.getTempFileURL({
          fileList: [trimmed],
          success: (res) => {
            resolve(res.fileList?.[0]?.tempFileURL || '');
          },
          fail: reject
        });
      });
    }
    
    // 存入缓存
    if (url) {
      urlCache.set(trimmed, { url, time: Date.now() });
    }
    
    return url;
  } catch (err) {
    console.error('[resolveFileURL] error:', err);
    // 缓存失败结果，避免重复请求
    urlCache.set(trimmed, { url: '', time: Date.now() });
    return '';
  }
};

/**
 * 批量将 fileID 转换为可访问的 URL
 * @param {string[]} fileIDs - fileID 数组
 * @returns {Promise<Map<string, string>>} fileID -> URL 映射
 */
export const resolveFileURLs = async (fileIDs) => {
  const result = new Map();
  const needResolve = [];
  
  // 分类：已缓存 / 需要解析
  for (const id of fileIDs) {
    if (!id || typeof id !== 'string' || id.trim() === '') {
      result.set(id, '');
      continue;
    }
    const trimmed = id.trim();
    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      result.set(trimmed, trimmed);
      continue;
    }
    if (!trimmed.startsWith('cloud://')) {
      result.set(trimmed, trimmed);
      continue;
    }
    const cached = urlCache.get(trimmed);
    if (cached && Date.now() - cached.time < URL_CACHE_TTL) {
      result.set(trimmed, cached.url);
    } else {
      needResolve.push(trimmed);
    }
  }
  
  if (needResolve.length === 0) {
    return result;
  }
  
  await waitForInit();
  
  try {
    let urls;
    
    if (process.env.TARO_ENV === 'h5') {
      if (!cloudInstance) {
        await initCloud();
      }
      if (!cloudInstance) {
        throw new Error('云开发未初始化');
      }
      await ensureLogin();
      
      // H5 环境：使用 fileList 参数
      const res = await cloudInstance.getTempFileURL({
        fileList: needResolve
      });
      urls = res.fileList || [];
    } else {
      // 小程序环境
      urls = await new Promise((resolve, reject) => {
        Taro.cloud.getTempFileURL({
          fileList: needResolve,
          success: (res) => resolve(res.fileList || []),
          fail: reject
        });
      });
    }
    
    // 更新缓存和结果
    for (let i = 0; i < needResolve.length; i++) {
      const id = needResolve[i];
      const url = urls[i]?.tempFileURL || urls[i]?.downloadUrl || '';
      urlCache.set(id, { url, time: Date.now() });
      result.set(id, url);
    }
    
    return result;
  } catch (err) {
    console.error('[resolveFileURLs] error:', err);
    // 失败时返回空字符串
    for (const id of needResolve) {
      result.set(id, '');
    }
    return result;
  }
};

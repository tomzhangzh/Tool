// src/utils/cloud.js
// 云开发环境初始化与适配层
import Taro from '@tarojs/taro';
import cloudbase from '@cloudbase/js-sdk';

// 替换为你自己的云开发环境 ID
const ENV_ID = 'cloud1-d9g0cl0c6e5006db7'; 

// 本地用户ID存储键（独立于CloudBase，确保身份切换可靠）
const LOCAL_USER_ID_KEY = 'weixinsport_local_uid';

let cloudInstance = null;
let isAnonymousLogin = false;
let initPromise = null;

// 生成UUID v4
const generateUUID = () => {
  try {
    if (crypto && crypto.randomUUID) {
      return crypto.randomUUID();
    }
  } catch (e) {}
  
  // 手动生成UUID v4
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

// 彻底清除所有浏览器存储（确保下次登录获得全新身份）
const clearAllBrowserStorage = () => {
  try {
    // 清除所有 localStorage
    const allLocalKeys = [];
    for (let i = 0; i < localStorage.length; i++) {
      allLocalKeys.push(localStorage.key(i));
    }
    allLocalKeys.forEach(key => {
      localStorage.removeItem(key);
    });
    console.log('[clearStorage] 清除 localStorage:', allLocalKeys.length, '项');

    // 清除所有 sessionStorage
    const allSessionKeys = [];
    for (let i = 0; i < sessionStorage.length; i++) {
      allSessionKeys.push(sessionStorage.key(i));
    }
    allSessionKeys.forEach(key => {
      sessionStorage.removeItem(key);
    });
    console.log('[clearStorage] 清除 sessionStorage:', allSessionKeys.length, '项');

    // 清除 IndexedDB
    try {
      if (indexedDB && indexedDB.databases) {
        const dbs = indexedDB.databases();
        dbs.forEach(db => {
          if (db.name) {
            indexedDB.deleteDatabase(db.name);
            console.log('[clearStorage] 删除 IndexedDB:', db.name);
          }
        });
      }
    } catch (e) {
      console.warn('[clearStorage] 清除 IndexedDB 失败:', e.message);
    }
  } catch (e) {
    console.warn('[clearStorage] 清除存储失败:', e.message);
  }
};

// 初始化云开发
export const initCloud = async () => {
  if (initPromise) {
    return initPromise;
  }
  
  initPromise = (async () => {
    if (process.env.TARO_ENV === 'h5') {
      // H5 环境：使用 CloudBase JS SDK npm 包
      if (cloudbase) {
        cloudInstance = cloudbase.init({
          env: ENV_ID
        });
        console.log('云开发 Web SDK 初始化成功 (H5)');
        
        // H5 环境需要匿名登录才能调用云函数
        try {
          const auth = cloudInstance.auth();
          const user = auth.getCurrentUser();
          if (!user) {
            console.log('[initCloud] 开始匿名登录...');
            await auth.signInAnonymously();
            isAnonymousLogin = true;
            console.log('[initCloud] 匿名登录成功');
          } else {
            isAnonymousLogin = true;
            console.log('[initCloud] 已有登录状态:', user.uid);
          }
        } catch (err) {
          console.error('[initCloud] 匿名登录失败:', err);
        }
      } else {
        console.warn('CloudBase JS SDK 加载失败');
      }
    } else {
      // 小程序环境：使用 Taro.cloud
      if (Taro.cloud) {
        Taro.cloud.init({
          env: ENV_ID
        });
        cloudInstance = Taro.cloud;
        console.log('云开发初始化成功 (小程序)');
      }
    }
  })();
  
  return initPromise;
};

// 等待初始化完成
export const waitForInit = () => initPromise || Promise.resolve();

// 获取云开发实例
export const getCloud = () => cloudInstance;

// 退出 CloudBase 登录（用于切换用户）
export const signOutCloudBase = async () => {
  if (process.env.TARO_ENV !== 'h5' || !cloudInstance) {
    isAnonymousLogin = false;
    initPromise = null;
    clearLocalUserId();
    return;
  }

  try {
    const auth = cloudInstance.auth();
    const user = auth.getCurrentUser();
    if (user) {
      await auth.signOut();
      console.log('[signOutCloudBase] 已退出 CloudBase 登录');
    }
  } catch (err) {
    console.warn('[signOutCloudBase] 退出失败:', err.message);
  }

  // 清除本地用户ID
  clearLocalUserId();

  // 彻底清除所有浏览器存储
  clearAllBrowserStorage();

  isAnonymousLogin = false;
  initPromise = null;
};

// 获取当前用户的业务ID（本地UUID，独立于CloudBase）
export const getCurrentUid = async () => {
  if (process.env.TARO_ENV === 'h5') {
    return getOrCreateLocalUserId();
  }
  return null;
};

// 确保 H5 环境已登录
const ensureLogin = async () => {
  if (process.env.TARO_ENV !== 'h5' || !cloudInstance) return;

  const auth = cloudInstance.auth();

  let user = auth.getCurrentUser();
  if (user) {
    isAnonymousLogin = true;
    return user;
  }

  try {
    const result = await auth.signInAnonymously();
    if (result.error) {
      throw new Error('匿名登录失败：' + result.error.message);
    }
    isAnonymousLogin = true;
    user = auth.getCurrentUser();
    console.log('[ensureLogin] 匿名登录成功:', user ? user.uid : 'null');
    return user;
  } catch (err) {
    console.error('[ensureLogin] 匿名登录失败:', err.message);
    throw new Error('匿名登录失败：' + (err.message || '请检查网络连接'));
  }
};

// 调用云函数（H5 和 小程序 统一接口）
export const callFunction = async (name, data = {}) => {
  await waitForInit();

  const cloud = getCloud();
  if (!cloud) {
    throw new Error('云开发未初始化');
  }

  if (process.env.TARO_ENV === 'h5') {
    await ensureLogin();

    // 使用本地用户ID作为业务标识
    const localUid = getOrCreateLocalUserId();
    console.log('[callFunction] name:', name, 'localUid:', localUid);
    const requestData = { ...data, _uid: localUid };

    try {
      const res = await cloud.callFunction({ name, data: requestData });
      return res;
    } catch (err) {
      console.error('[callFunction] error:', err);

      const errMsg = JSON.stringify(err);
      const isAuthError = err.error === 'unauthenticated' ||
        err.error_code === 'UNAUTHENTICATED' ||
        errMsg.includes('unauthenticated') ||
        errMsg.includes('credentials not found');

      if (isAuthError) {
        console.log('[callFunction] 认证失效，重新登录后重试...');
        try {
          await ensureLogin();
          const retryData = { ...data, _uid: localUid };
          return cloud.callFunction({ name, data: retryData });
        } catch (retryErr) {
          console.error('[callFunction] 重试失败:', retryErr);
          throw new Error('登录状态失效，请点击重试');
        }
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
    const cloud = getCloud();
    if (!cloud) {
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
    
    const uploadRes = await cloud.uploadFile({
      cloudPath: safePath,
      fileContent
    });
    
    const downloadUrl = await cloud.getTempFileURL({
      fileIDs: [uploadRes.fileID]
    });
    
    return {
      fileID: uploadRes.fileID,
      url: downloadUrl.fileList?.[0]?.tempFileURL || ''
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

// src/utils/cloud.js
// 云开发环境初始化与适配层
import Taro from '@tarojs/taro';
import cloudbase from '@cloudbase/js-sdk';

// 替换为你自己的云开发环境 ID
const ENV_ID = 'cloud1-d9g0cl0c6e5006db7'; 

let cloudInstance = null;
let isAnonymousLogin = false;
let initPromise = null;
let cachedUid = null;  // 缓存用户 uid

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
          // 登录后获取并缓存用户 uid
          cachedUid = parseUidFromStorage();
          console.log('[initCloud] uid:', cachedUid);
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

// 从 localStorage 中解析 uid
const parseUidFromStorage = () => {
  try {
    // 遍历所有 localStorage 键，查找 CloudBase 存储的用户信息
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      // CloudBase 用户信息存储在 user_info_<envId> 键中
      if (key && key.startsWith('user_info_')) {
        const rawValue = localStorage.getItem(key);
        if (rawValue) {
          try {
            const parsed = JSON.parse(rawValue);
            // 格式: {"version":"localCachev1","content":{"uid":"xxx",...}}
            if (parsed && parsed.content && parsed.content.uid) {
              console.log('[parseUidFromStorage] found uid:', parsed.content.uid);
              return parsed.content.uid;
            }
            // 兼容其他格式
            if (parsed && parsed.content && typeof parsed.content === 'string' && parsed.content.length > 10) {
              console.log('[parseUidFromStorage] found uid (string):', parsed.content);
              return parsed.content;
            }
          } catch {
            // 忽略 JSON 解析错误
          }
        }
      }
    }
    // 备选：查找 anonymous_uuid 键
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.includes('anonymous_uuid')) {
        const rawValue = localStorage.getItem(key);
        if (rawValue) {
          try {
            const parsed = JSON.parse(rawValue);
            if (parsed && parsed.content) {
              console.log('[parseUidFromStorage] found uid from anonymous_uuid:', parsed.content);
              return parsed.content;
            }
          } catch {
            if (rawValue.length > 10) return rawValue;
          }
        }
      }
    }
  } catch (err) {
    console.error('parseUidFromStorage error:', err);
  }
  return null;
};

// 获取当前用户的 uid
export const getCurrentUid = async () => {
  if (process.env.TARO_ENV === 'h5') {
    // 如果已经缓存了 uid，直接返回
    if (cachedUid) {
      return cachedUid;
    }

    // 先尝试从 auth.getCurrentUser 获取
    if (cloudInstance) {
      try {
        const auth = cloudInstance.auth();
        const user = auth.getCurrentUser();
        if (user) {
          const foundUid = user.uid || user.openid || user._uid || user.id || null;
          if (foundUid) {
            console.log('[getCurrentUid] found uid from auth:', foundUid);
            cachedUid = foundUid;
            return foundUid;
          }
        }
      } catch (err) {
        console.warn('getCurrentUid auth error:', err.message);
      }
    }

    // 备选：从 localStorage 解析
    const uid = parseUidFromStorage();
    if (uid) {
      console.log('[getCurrentUid] found uid from localStorage:', uid);
      cachedUid = uid;
      return uid;
    }
  }
  console.log('[getCurrentUid] returning null');
  return null;
};

// 确保 H5 环境已登录
const ensureLogin = async () => {
  if (process.env.TARO_ENV !== 'h5' || !cloudInstance) return;

  const auth = cloudInstance.auth();

  // 检查当前登录状态
  let user = auth.getCurrentUser();
  console.log('[ensureLogin] current user:', user);
  if (user) {
    console.log('[ensureLogin] user.uid:', user.uid);
  }

  if (!user || !user.uid) {
    console.log('[ensureLogin] 用户未登录或无uid，进行匿名登录...');
    try {
      const result = await auth.signInAnonymously();
      console.log('[ensureLogin] signInAnonymously result:', result);
      
      // 检查返回的错误
      if (result.error) {
        console.error('[ensureLogin] signInAnonymously error:', result.error);
        throw new Error('匿名登录失败：' + result.error.message);
      }
      
      // 从 result.user 获取用户信息
      const loginUser = result.user;
      if (!loginUser) {
        console.error('[ensureLogin] signInAnonymously no user:', result);
        throw new Error('匿名登录返回数据无效');
      }
      
      const uid = loginUser.uid || loginUser._uid || loginUser.id;
      if (!uid) {
        console.error('[ensureLogin] signInAnonymously user no uid:', loginUser);
        throw new Error('匿名登录用户无uid');
      }
      
      isAnonymousLogin = true;
      cachedUid = uid;
      console.log('[ensureLogin] uid from result:', uid);
      
      // 再次获取用户确认
      user = auth.getCurrentUser();
      console.log('[ensureLogin] user after login:', user ? user.uid : 'null');
      console.log('[ensureLogin] 匿名登录成功');
    } catch (err) {
      console.error('[ensureLogin] 匿名登录失败:', err.message);
      throw new Error('匿名登录失败：' + (err.message || '请检查网络连接'));
    }
  } else {
    isAnonymousLogin = true;
    console.log('[ensureLogin] 已有登录状态:', user.uid);
  }
  
  return user;
};

// 调用云函数（H5 和 小程序 统一接口）
export const callFunction = async (name, data = {}) => {
  // 等待初始化完成
  await waitForInit();

  const cloud = getCloud();
  if (!cloud) {
    throw new Error('云开发未初始化');
  }

  if (process.env.TARO_ENV === 'h5') {
    // 确保已登录
    await ensureLogin();

    // H5 环境：获取当前用户 uid 并添加到请求数据
    const uid = await getCurrentUid();
    console.log('[callFunction] name:', name, 'uid:', uid);
    const requestData = { ...data, _uid: uid };

    // CloudBase JS SDK 的 callFunction
    try {
      const res = await cloud.callFunction({ name, data: requestData });
      return res;
    } catch (err) {
      console.error('[callFunction] error:', err);

      // 如果是未认证错误，尝试重新登录并重试
      const errMsg = JSON.stringify(err);
      const isAuthError = err.error === 'unauthenticated' ||
        err.error_code === 'UNAUTHENTICATED' ||
        errMsg.includes('unauthenticated') ||
        errMsg.includes('credentials not found');

      if (isAuthError) {
        console.log('[callFunction] 认证失效，重新登录后重试...');
        try {
          await ensureLogin();
          const retryUid = await getCurrentUid();
          const retryData = { ...data, _uid: retryUid };
          return cloud.callFunction({ name, data: retryData });
        } catch (retryErr) {
          console.error('[callFunction] 重试失败:', retryErr);
          throw new Error('登录状态失效，请点击重试');
        }
      }

      // 其他错误直接抛出
      throw err;
    }
  } else {
    // 小程序环境：Taro.cloud.callFunction
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
    // H5 环境：使用 CloudBase JS SDK 上传
    const cloud = getCloud();
    if (!cloud) {
      throw new Error('云开发未初始化');
    }
    await ensureLogin();
    
    // 确保 cloudPath 格式正确
    const safePath = cloudPath.replace(/[^a-zA-Z0-9_./-]/g, '_');
    
    // 将 Blob/File 转换为 ArrayBuffer 上传
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
    
    // 获取临时下载链接
    const downloadUrl = await cloud.getTempFileURL({
      fileIDs: [uploadRes.fileID]
    });
    
    return {
      fileID: uploadRes.fileID,
      url: downloadUrl.fileList?.[0]?.tempFileURL || ''
    };
  } else {
    // 小程序环境：使用 Taro.cloud.uploadFile
    return new Promise((resolve, reject) => {
      Taro.cloud.uploadFile({
        cloudPath,
        filePath: file,
        success: (res) => {
          resolve({
            fileID: res.fileID,
            url: res.fileID // 小程序中 fileID 可直接使用
          });
        },
        fail: reject
      });
    });
  }
};

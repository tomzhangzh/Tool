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
          const loginState = auth.hasLoginState();
          if (!loginState) {
            await auth.anonymousAuthProvider().signIn();
            isAnonymousLogin = true;
          } else {
            isAnonymousLogin = true;
          }
          // 登录后获取并缓存用户 uid
          cachedUid = parseUidFromStorage();
        } catch (err) {
          console.warn('H5 匿名登录失败:', err.message);
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
    // 直接从 localStorage 解析 uid
    const uid = parseUidFromStorage();
    if (uid) {
      console.log('[getCurrentUid] found uid from localStorage:', uid);
      return uid;
    }
    // 备选方案：从 auth.getCurrentUser 获取
    if (cloudInstance) {
      try {
        const auth = cloudInstance.auth();
        if (auth.hasLoginState()) {
          const user = auth.getCurrentUser();
          if (user) {
            const foundUid = user.uid || user.openid || user._uid || user.id || null;
            if (foundUid) {
              console.log('[getCurrentUid] found uid from auth:', foundUid);
              return foundUid;
            }
          }
        }
      } catch (err) {
        console.error('getCurrentUid error:', err);
      }
    }
  }
  console.log('[getCurrentUid] returning null');
  return null;
};

// 确保 H5 环境已登录
const ensureLogin = async () => {
  if (process.env.TARO_ENV !== 'h5' || !cloudInstance) return;
  
  const auth = cloudInstance.auth();
  if (!auth.hasLoginState()) {
    console.log('[ensureLogin] 重新进行匿名登录...');
    await auth.anonymousAuthProvider().signIn();
    isAnonymousLogin = true;
    cachedUid = null; // 重置缓存
    console.log('[ensureLogin] 匿名登录成功');
  }
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
      // 如果是未认证错误，尝试重新登录并重试
      if (err.error === 'unauthenticated' || err.error_code === 'UNAUTHENTICATED') {
        console.log('[callFunction] 认证失效，重新登录后重试...');
        await ensureLogin();
        const retryUid = await getCurrentUid();
        const retryData = { ...data, _uid: retryUid };
        return cloud.callFunction({ name, data: retryData });
      }
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

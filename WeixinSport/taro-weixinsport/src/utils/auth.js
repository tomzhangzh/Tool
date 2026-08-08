// src/utils/auth.js
// 登录与鉴权处理，特别是 H5 环境下的登录状态管理
import Taro from '@tarojs/taro';
import { signOutCloudBase } from './cloud';

const USER_INFO_KEY = 'weixinsport_user_info';
const CLOUD_LOGIN_KEY = 'weixinsport_cloud_login';

// 存储用户信息
export const setUserInfo = (userInfo) => {
  if (userInfo) {
    // 清除密码后再存储（安全考虑）
    const safeInfo = { ...userInfo };
    delete safeInfo.password;
    
    // 确保 role 字段存在且正确
    const role = safeInfo.role || null;
    const openid = safeInfo.openid || null;
    
    Taro.setStorageSync(USER_INFO_KEY, safeInfo);
    Taro.setStorageSync('role', role);
    Taro.setStorageSync('openid', openid);
    
    // 记录 CloudBase 已登录状态
    Taro.setStorageSync(CLOUD_LOGIN_KEY, Date.now());
    
    console.log('[setUserInfo] 存储用户信息:', { role, openid, name: safeInfo.name });
  }
};

// 获取用户信息
export const getUserInfo = () => {
  try {
    return Taro.getStorageSync(USER_INFO_KEY) || null;
  } catch (e) {
    return null;
  }
};

// 获取角色
export const getRole = () => {
  try {
    // 优先从单独的 role 键读取
    const role = Taro.getStorageSync('role');
    if (role) {
      console.log('[getRole] 从 role 键获取:', role);
      return role;
    }
    // 容错：从 userInfo 中获取 role
    const userInfo = Taro.getStorageSync(USER_INFO_KEY);
    if (userInfo && userInfo.role) {
      console.log('[getRole] 从 userInfo 获取:', userInfo.role);
      // 同步到 role 键
      Taro.setStorageSync('role', userInfo.role);
      return userInfo.role;
    }
    console.warn('[getRole] 未找到角色信息');
    return null;
  } catch (e) {
    console.error('[getRole] 错误:', e);
    return null;
  }
};

// 获取 openid
export const getOpenid = () => {
  try {
    return Taro.getStorageSync('openid') || null;
  } catch (e) {
    return null;
  }
};

// 检查是否已登录（用于自动登录判断）
export const isLoggedIn = () => {
  try {
    const userInfo = Taro.getStorageSync(USER_INFO_KEY);
    const openid = Taro.getStorageSync('openid');
    return !!(userInfo && openid);
  } catch (e) {
    return false;
  }
};

// 清除登录状态（退出登录时调用）
export const clearLogin = async () => {
  try {
    Taro.removeStorageSync(USER_INFO_KEY);
    Taro.removeStorageSync('role');
    Taro.removeStorageSync('openid');
    Taro.removeStorageSync(CLOUD_LOGIN_KEY);
    // 退出 CloudBase 登录，确保下次匿名登录获得新 uid
    await signOutCloudBase();
  } catch (e) {
    console.warn('[clearLogin] 退出异常:', e);
  }
};

/**
 * H5 环境下：处理微信网页授权回调
 * 微信授权后会重定向回 redirect_uri，URL 中带有 code 参数
 */
export const handleWxCallback = async () => {
  if (process.env.TARO_ENV !== 'h5') return null;

  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get('code');
  
  if (code) {
    console.log('检测到微信授权 code，开始换取 openid...');
    try {
      // 这里需要调用云函数来换取 openid
      // TODO: 实现微信网页授权换取 openid
      
      // 清理 URL 中的 code 参数
      window.history.replaceState({}, document.title, window.location.pathname);
      return null;
    } catch (e) {
      console.error('换取 openid 失败', e);
      return null;
    }
  }
  return null;
};

/**
 * H5 环境下：发起微信网页授权
 * @param {string} redirectUri 回调地址
 */
export const redirectToWxAuth = (redirectUri) => {
  if (process.env.TARO_ENV !== 'h5') return;
  
  // 确保在微信浏览器中
  const ua = navigator.userAgent.toLowerCase();
  if (ua.indexOf('micromessenger') === -1) {
    console.warn('非微信环境，无法使用微信授权');
    return;
  }

  // 构造微信授权 URL
  // 注意：这里的 APPID 是公众号的 AppID，不是小程序的
  const APPID = 'YOUR_WECHAT_APPID'; 
  const scope = 'snsapi_userinfo';
  const state = 'wx_auth_state';
  
  const authUrl = `https://open.weixin.qq.com/connect/oauth2/authorize?appid=${APPID}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${scope}&state=${state}#wechat_redirect`;
  
  window.location.href = authUrl;
};

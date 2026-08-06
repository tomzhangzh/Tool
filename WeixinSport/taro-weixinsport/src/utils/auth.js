// src/utils/auth.js
// 登录与鉴权处理，特别是 H5 环境下的微信网页授权
import Taro from '@tarojs/taro';
import api from './api';

// 模拟一个登录状态管理
// 正式项目建议使用 Redux/MobX/Zustand 等状态管理库

const USER_INFO_KEY = 'weixinsport_user_info';

// 存储用户信息
export const setUserInfo = (userInfo) => {
  if (userInfo) {
    Taro.setStorageSync(USER_INFO_KEY, userInfo);
    Taro.setStorageSync('role', userInfo.role);
    Taro.setStorageSync('openid', userInfo.openid);
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
    return Taro.getStorageSync('role') || null;
  } catch (e) {
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

// 清除登录状态
export const clearLogin = () => {
  try {
    Taro.removeStorageSync(USER_INFO_KEY);
    Taro.removeStorageSync('role');
    Taro.removeStorageSync('openid');
  } catch (e) {}
};

/**
 * H5 环境下：处理微信网页授权回调
 * 微信授权后会重定向回 redirect_uri，URL 中带有 code 参数
 * 我们需要用 code 换取 access_token 和 openid
 */
export const handleWxCallback = async () => {
  if (process.env.TARO_ENV !== 'h5') return null;

  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get('code');
  
  if (code) {
    console.log('检测到微信授权 code，开始换取 openid...');
    try {
      // 这里需要调用云函数来换取 openid，因为需要 AppSecret
      // 我们可以扩展 login 云函数，增加一个 h5Login 的 action
      const userInfo = await api.login(); // 这个需要在云函数中适配
      setUserInfo(userInfo);
      
      // 清理 URL 中的 code 参数，避免刷新时重复使用
      window.history.replaceState({}, document.title, window.location.pathname);
      return userInfo;
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
    // 可以降级为其他登录方式
    return;
  }

  // 构造微信授权 URL
  // 注意：这里的 APPID 是公众号的 AppID，不是小程序的
  const APPID = 'YOUR_WECHAT_APPID'; 
  const scope = 'snsapi_userinfo'; // 或 snsapi_base
  const state = 'wx_auth_state'; // 可自定义
  
  const authUrl = `https://open.weixin.qq.com/connect/oauth2/authorize?appid=${APPID}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${scope}&state=${state}#wechat_redirect`;
  
  window.location.href = authUrl;
};

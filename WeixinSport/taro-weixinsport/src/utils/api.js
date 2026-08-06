// src/utils/api.js
// 统一 API 层：兼容小程序 (Taro.cloud) 和 H5 (CloudBase JS SDK)
import Taro from '@tarojs/taro';
import { callFunction } from './cloud';

/**
 * 调用云函数
 * @param {string} name 云函数名
 * @param {object} data 入参
 * @returns {Promise<any>}
 */
export const call = async (name, data = {}) => {
  try {
    const res = await callFunction(name, data);
    
    // H5 环境：CloudBase JS SDK 返回的格式
    // 小程序环境：Taro.cloud 返回的格式
    const result = res.result || res;
    
    if (result && result.code === 0) {
      return result.data;
    } else {
      const msg = (result && result.message) || '请求失败';
      Taro.showToast({ title: msg, icon: 'none' });
      throw new Error(msg);
    }
  } catch (err) {
    console.error('callFunction fail:', err);
    if (err.message === '云开发未初始化') {
      Taro.showToast({ title: '云开发未初始化', icon: 'none' });
    } else {
      Taro.showToast({ title: '网络错误', icon: 'none' });
    }
    throw err;
  }
};

// --- 登录/账号 ---
export const login = () => call('login', {});
export const bindRole = (data) => call('login', { action: 'bindRole', ...data });
export const getProfile = () => call('login', { action: 'getProfile' });
export const updateProfile = (data) => call('login', { action: 'updateProfile', ...data });

// --- 班级 ---
export const createClass = (data) => call('class', { action: 'create', ...data });
export const joinClass = (data) => call('class', { action: 'join', ...data });
export const getClassDetail = (classId) => call('class', { action: 'detail', classId });
export const getClassMembers = (classId) => call('class', { action: 'members', classId });
export const getClassTeachers = (classId) => call('class', { action: 'listTeachers', classId });
export const addTeacher = (code) => call('class', { action: 'addTeacher', code });
export const removeTeacher = (classId, targetOpenid) => call('class', { action: 'removeTeacher', classId, targetOpenid });
export const getMyClasses = () => call('class', { action: 'my' });
export const bindChild = (data) => call('class', { action: 'bindChild', ...data });

// --- 打卡 ---
export const submitCheckin = (data) => call('checkin', { action: 'submit', ...data });
export const getCheckinList = (data) => call('checkin', { action: 'list', ...data });
export const deleteCheckin = (id) => call('checkin', { action: 'delete', id });
export const getTodayCheckin = () => call('checkin', { action: 'today' });

// --- 统计 ---
export const getWeeklyStats = (data) => call('stats', { action: 'weekly', ...data });
export const getMonthlyStats = (data) => call('stats', { action: 'monthly', ...data });
export const getRanking = (data) => call('stats', { action: 'ranking', ...data });

// --- 奖项 ---
export const getWeeklyAwards = (data) => call('awards', { action: 'weekly', ...data });
export const getMonthlyStars = (data) => call('awards', { action: 'monthly', ...data });
export const getMyAwards = (data) => call('awards', { action: 'mine', ...data });
export const calcWeeklyAwards = (data) => call('awards', { action: 'calcWeekly', ...data });
export const calcMonthlyStars = (data) => call('awards', { action: 'calcMonthly', ...data });

export default {
  login, bindRole, getProfile, updateProfile,
  createClass, joinClass, getClassDetail, getClassMembers, getClassTeachers, addTeacher, removeTeacher, getMyClasses, bindChild,
  submitCheckin, getCheckinList, deleteCheckin, getTodayCheckin,
  getWeeklyStats, getMonthlyStats, getRanking,
  getWeeklyAwards, getMonthlyStars, getMyAwards, calcWeeklyAwards, calcMonthlyStars
};

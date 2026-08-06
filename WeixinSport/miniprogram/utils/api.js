// utils/api.js
// 统一封装云函数调用，便于错误处理与加载态管理

/**
 * 调用云函数
 * @param {string} name 云函数名
 * @param {object} data 入参
 */
const call = (name, data = {}) => {
  return new Promise((resolve, reject) => {
    wx.cloud.callFunction({
      name,
      data,
      success: res => {
        if (res.result && res.result.code === 0) {
          resolve(res.result.data);
        } else {
          const msg = (res.result && res.result.message) || '请求失败';
          wx.showToast({ title: msg, icon: 'none' });
          reject(new Error(msg));
        }
      },
      fail: err => {
        wx.showToast({ title: '网络错误', icon: 'none' });
        reject(err);
      }
    });
  });
};

// 登录/账号
const login = () => call('login', {});
const bindRole = (data) => call('login', { action: 'bindRole', ...data });
const getProfile = () => call('login', { action: 'getProfile' });
const updateProfile = (data) => call('login', { action: 'updateProfile', ...data });
const dedupUsers = () => call('login', { action: 'dedupUsers' });

// 班级
const createClass = (data) => call('class', { action: 'create', ...data });
const joinClass = (data) => call('class', { action: 'join', ...data });
const getClassDetail = (classId) => call('class', { action: 'detail', classId });
const getClassMembers = (classId) => call('class', { action: 'members', classId });
const getClassTeachers = (classId) => call('class', { action: 'listTeachers', classId });
const addTeacher = (code) => call('class', { action: 'addTeacher', code });
const removeTeacher = (classId, targetOpenid) => call('class', { action: 'removeTeacher', classId, targetOpenid });
const getMyClasses = () => call('class', { action: 'my' });
const bindChild = (data) => call('class', { action: 'bindChild', ...data });

// 打卡
const submitCheckin = (data) => call('checkin', { action: 'submit', ...data });
const getCheckinList = (data) => call('checkin', { action: 'list', ...data });
const deleteCheckin = (id) => call('checkin', { action: 'delete', id });
const getTodayCheckin = () => call('checkin', { action: 'today' });

// 统计
const getWeeklyStats = (data) => call('stats', { action: 'weekly', ...data });
const getMonthlyStats = (data) => call('stats', { action: 'monthly', ...data });
const getRanking = (data) => call('stats', { action: 'ranking', ...data });

// 奖项
const getWeeklyAwards = (data) => call('awards', { action: 'weekly', ...data });
const getMonthlyStars = (data) => call('awards', { action: 'monthly', ...data });
const getMyAwards = (data) => call('awards', { action: 'mine', ...data });
const calcWeeklyAwards = (data) => call('awards', { action: 'calcWeekly', ...data });
const calcMonthlyStars = (data) => call('awards', { action: 'calcMonthly', ...data });

module.exports = {
  call,
  login,
  bindRole,
  getProfile,
  updateProfile,
  dedupUsers,
  createClass,
  joinClass,
  getClassDetail,
  getClassMembers,
  getClassTeachers,
  addTeacher,
  removeTeacher,
  getMyClasses,
  bindChild,
  submitCheckin,
  getCheckinList,
  deleteCheckin,
  getTodayCheckin,
  getWeeklyStats,
  getMonthlyStats,
  getRanking,
  getWeeklyAwards,
  getMonthlyStars,
  getMyAwards,
  calcWeeklyAwards,
  calcMonthlyStars
};

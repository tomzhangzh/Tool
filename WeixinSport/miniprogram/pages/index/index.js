// pages/index/index.js
const app = getApp();
const api = require('../../utils/api.js');
const { ROLE_LABEL } = require('../../utils/constants.js');
const util = require('../../utils/util.js');

Page({
  data: {
    role: '',
    userInfo: null,
    loading: true,
    // 学生数据
    todayCheckin: [],
    weekStats: null,
    myAwards: [],
    // 老师数据
    classList: [],
    // 家长数据
    childInfo: null,
    childWeekStats: null
  },

  onShow() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    this.setData({ role: app.globalData.role, userInfo: app.globalData.userInfo });
    this.loadData();
  },

  onPullDownRefresh() {
    this.loadData().finally(() => wx.stopPullDownRefresh());
  },

  async loadData() {
    this.setData({ loading: true });
    const role = app.globalData.role;
    try {
      if (role === 'student') {
        await this.loadStudentData();
      } else if (role === 'teacher') {
        await this.loadTeacherData();
      } else if (role === 'parent') {
        await this.loadParentData();
      }
    } catch (e) {
      console.error('load home data error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  // 学生首页
  async loadStudentData() {
    const [today, week, awards] = await Promise.all([
      api.getTodayCheckin().catch(() => []),
      api.getWeeklyStats({}).catch(() => null),
      api.getMyAwards({ limit: 3 }).catch(() => [])
    ]);
    this.setData({ todayCheckin: today, weekStats: week, myAwards: awards });
  },

  // 老师首页
  async loadTeacherData() {
    const list = await api.getMyClasses().catch(() => []);
    this.setData({ classList: list });
  },

  // 家长首页
  async loadParentData() {
    // 家长视图复用孩子统计
    const week = await api.getWeeklyStats({ asParent: true }).catch(() => null);
    const childInfo = week ? week.user : null;
    this.setData({ childWeekStats: week, childInfo });
  },

  goCheckin() {
    wx.navigateTo({ url: '/pages/checkin/checkin' });
  },

  goCheckinList() {
    wx.navigateTo({ url: '/pages/checkin-list/checkin-list' });
  },

  goWeekly() {
    wx.switchTab({ url: '/pages/weekly/weekly' });
  },

  goMonthly() {
    wx.switchTab({ url: '/pages/monthly/monthly' });
  },

  goStats() {
    wx.navigateTo({ url: '/pages/stats/stats' });
  },

  goRanking() {
    wx.navigateTo({ url: '/pages/ranking/ranking' });
  },

  goClass() {
    wx.navigateTo({ url: '/pages/class/class' });
  },

  goAwards() {
    wx.navigateTo({ url: '/pages/awards/awards' });
  },

  // 分享给微信好友/群聊
  onShareAppMessage() {
    const userInfo = this.data.userInfo || {};
    const role = this.data.role;
    if (role === 'teacher') {
      return {
        title: `${userInfo.name || '老师'}邀请你加入运动班级，一起运动打卡吧！`,
        path: '/pages/index/index',
        imageUrl: '' // 可换成自定义分享图
      };
    }
    return {
      title: `${userInfo.name || '我'}在「运动小达人」坚持运动打卡，快来一起拿奖吧！`,
      path: '/pages/index/index',
      imageUrl: ''
    };
  },

  // 分享到朋友圈
  onShareTimeline() {
    const userInfo = this.data.userInfo || {};
    return {
      title: `运动小达人：每周评选运动之星，月度明星等你来拿！`,
      query: ''
    };
  }
});

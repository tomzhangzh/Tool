// pages/weekly/weekly.js
const app = getApp();
const api = require('../../utils/api.js');
const util = require('../../utils/util.js');
const { WEEKLY_AWARD_TYPES, ROLE_LABEL } = require('../../utils/constants.js');

Page({
  data: {
    role: '',
    weekOffset: 0, // 0=本周, -1=上周
    weekLabel: '',
    awards: [], // 各奖项 [{ awardType, awardName, awardIcon, desc, winners: [] }]
    loading: true,
    canCalc: false // 老师可触发计算
  },

  onLoad() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    this.setData({ role: app.globalData.role });
    this.loadAwards();
  },

  onShow() {
    if (app.globalData.userInfo) this.loadAwards();
  },

  onPullDownRefresh() {
    this.loadAwards().finally(() => wx.stopPullDownRefresh());
  },

  changeWeek(e) {
    const offset = Number(e.currentTarget.dataset.offset);
    if (offset === this.data.weekOffset) return;
    this.setData({ weekOffset: offset });
    this.loadAwards();
  },

  async loadAwards() {
    this.setData({ loading: true });
    try {
      const data = await api.getWeeklyAwards({ weekOffset: this.data.weekOffset });
      // 补全所有奖项类型，未产生奖项的也显示"暂无"
      const awardsMap = {};
      (data.awards || []).forEach(a => {
        awardsMap[a.awardType] = a;
      });
      const awards = WEEKLY_AWARD_TYPES.map(t => {
        return awardsMap[t.id] || {
          awardType: t.id,
          awardName: t.name,
          awardIcon: t.icon,
          desc: t.desc,
          winners: []
        };
      });
      this.setData({
        weekLabel: data.weekLabel,
        awards,
        canCalc: data.canCalc
      });
    } catch (e) {
      console.error('load weekly awards error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  // 老师手动触发本周评选
  async onCalc() {
    const res = await new Promise(resolve => {
      wx.showModal({
        title: '生成本周评选',
        content: '将根据本周所有同学的打卡数据生成各项评选结果，是否继续？',
        success: r => resolve(r.confirm)
      });
    });
    if (!res) return;
    wx.showLoading({ title: '计算中...' });
    try {
      await api.calcWeeklyAwards({ weekOffset: this.data.weekOffset });
      wx.hideLoading();
      wx.showToast({ title: '评选已生成', icon: 'success' });
      this.loadAwards();
    } catch (e) {
      wx.hideLoading();
    }
  }
});

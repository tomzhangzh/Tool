// pages/monthly/monthly.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    role: '',
    monthOffset: 0, // 0=本月, -1=上月
    monthLabel: '',
    top3: [], // 月度前三名
    specialAwards: [], // 月度坚持之星、进步之星等
    loading: true,
    canCalc: false
  },

  onLoad() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    this.setData({ role: app.globalData.role });
    this.loadStars();
  },

  onShow() {
    if (app.globalData.userInfo) this.loadStars();
  },

  onPullDownRefresh() {
    this.loadStars().finally(() => wx.stopPullDownRefresh());
  },

  changeMonth(e) {
    const offset = Number(e.currentTarget.dataset.offset);
    if (offset === this.data.monthOffset) return;
    this.setData({ monthOffset: offset });
    this.loadStars();
  },

  async loadStars() {
    this.setData({ loading: true });
    try {
      const data = await api.getMonthlyStars({ monthOffset: this.data.monthOffset });
      this.setData({
        monthLabel: data.monthLabel,
        top3: data.top3 || [],
        specialAwards: data.specialAwards || [],
        canCalc: data.canCalc
      });
    } catch (e) {
      console.error('load monthly stars error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  async onCalc() {
    const res = await new Promise(resolve => {
      wx.showModal({
        title: '生成月度明星',
        content: '将根据本月综合得分评选月度明星，是否继续？',
        success: r => resolve(r.confirm)
      });
    });
    if (!res) return;
    wx.showLoading({ title: '计算中...' });
    try {
      await api.calcMonthlyStars({ monthOffset: this.data.monthOffset });
      wx.hideLoading();
      wx.showToast({ title: '已生成', icon: 'success' });
      this.loadStars();
    } catch (e) {
      wx.hideLoading();
    }
  }
});

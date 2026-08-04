// pages/class-detail/class-detail.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    classId: '',
    info: null,
    members: [],
    loading: true,
    role: ''
  },

  onLoad(options) {
    this.setData({ classId: options.id, role: app.globalData.role });
    this.loadDetail();
  },

  onShow() {
    if (this.data.classId) this.loadDetail();
  },

  async loadDetail() {
    this.setData({ loading: true });
    try {
      const [info, members] = await Promise.all([
        api.getClassDetail(this.data.classId),
        api.getClassMembers(this.data.classId)
      ]);
      this.setData({ info, members });
    } catch (e) {
      console.error('load class detail error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  copyCode() {
    wx.setClipboardData({
      data: this.data.info.code,
      success: () => wx.showToast({ title: '已复制邀请码', icon: 'success' })
    });
  },

  viewStats() {
    wx.navigateTo({ url: `/pages/stats/stats?classId=${this.data.classId}` });
  },

  viewRanking() {
    wx.navigateTo({ url: `/pages/ranking/ranking?classId=${this.data.classId}` });
  }
});

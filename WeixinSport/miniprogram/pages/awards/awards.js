// pages/awards/awards.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    awards: [],
    loading: true,
    filter: 'all', // all | weekly | monthly
    stats: { total: 0, gold: 0, silver: 0, bronze: 0 }
  },

  onLoad() {
    this.loadAwards();
  },

  onShow() {
    if (app.globalData.userInfo) this.loadAwards();
  },

  onPullDownRefresh() {
    this.loadAwards().finally(() => wx.stopPullDownRefresh());
  },

  switchFilter(e) {
    const filter = e.currentTarget.dataset.filter;
    if (filter === this.data.filter) return;
    this.setData({ filter });
    this.loadAwards();
  },

  async loadAwards() {
    this.setData({ loading: true });
    try {
      const data = await api.getMyAwards({ type: this.data.filter === 'all' ? undefined : this.data.filter });
      // 统计奖牌数
      const stats = { total: data.length, gold: 0, silver: 0, bronze: 0 };
      data.forEach(a => {
        if (a.rank === 1) stats.gold++;
        else if (a.rank === 2) stats.silver++;
        else if (a.rank === 3) stats.bronze++;
      });
      this.setData({ awards: data, stats });
    } catch (e) {
      console.error('load awards error', e);
    } finally {
      this.setData({ loading: false });
    }
  }
});

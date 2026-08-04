// pages/ranking/ranking.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    type: 'week', // week | month
    metric: 'calorie', // calorie | duration | frequency
    ranking: [],
    myRank: null,
    loading: true,
    metrics: [
      { id: 'calorie', name: '卡路里', unit: '千卡', icon: '🔥' },
      { id: 'duration', name: '运动时长', unit: '分钟', icon: '⏱️' },
      { id: 'frequency', name: '打卡天数', unit: '天', icon: '📅' }
    ]
  },

  onLoad() {
    this.loadRanking();
  },

  onShow() {
    if (app.globalData.userInfo) this.loadRanking();
  },

  switchType(e) {
    const type = e.currentTarget.dataset.type;
    if (type === this.data.type) return;
    this.setData({ type });
    this.loadRanking();
  },

  switchMetric(e) {
    const metric = e.currentTarget.dataset.metric;
    if (metric === this.data.metric) return;
    this.setData({ metric });
    this.loadRanking();
  },

  async loadRanking() {
    this.setData({ loading: true });
    try {
      const data = await api.getRanking({
        type: this.data.type,
        metric: this.data.metric
      });
      this.setData({
        ranking: data.ranking || [],
        myRank: data.myRank
      });
    } catch (e) {
      console.error('load ranking error', e);
    } finally {
      this.setData({ loading: false });
    }
  }
});

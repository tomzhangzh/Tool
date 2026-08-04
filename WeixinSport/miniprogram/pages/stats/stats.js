// pages/stats/stats.js
const app = getApp();
const api = require('../../utils/api.js');
const util = require('../../utils/util.js');

Page({
  data: {
    type: 'week', // week | month
    stats: null,
    dailyData: [], // 每日打卡数据（用于柱状图）
    exerciseDistribution: [], // 运动类型分布
    loading: true,
    maxVal: 100 // 柱状图最大值
  },

  onLoad(options) {
    if (options.type) this.setData({ type: options.type });
    this.loadStats();
  },

  switchType(e) {
    const type = e.currentTarget.dataset.type;
    if (type === this.data.type) return;
    this.setData({ type });
    this.loadStats();
  },

  async loadStats() {
    this.setData({ loading: true });
    try {
      const payload = this.data.type === 'week'
        ? { weekOffset: 0 }
        : { monthOffset: 0 };
      const fn = this.data.type === 'week' ? api.getWeeklyStats : api.getMonthlyStats;
      const data = await fn(payload);
      // 计算最大值用于柱状图缩放
      const maxVal = (data.dailyData || []).reduce((m, d) => Math.max(m, d.duration || 0, d.calorie || 0), 1) || 1;
      this.setData({
        stats: data,
        dailyData: data.dailyData || [],
        exerciseDistribution: data.exerciseDistribution || [],
        maxVal
      });
    } catch (e) {
      console.error('load stats error', e);
    } finally {
      this.setData({ loading: false });
    }
  }
});

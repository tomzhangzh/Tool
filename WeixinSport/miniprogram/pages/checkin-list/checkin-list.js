// pages/checkin-list/checkin-list.js
const app = getApp();
const api = require('../../utils/api.js');
const util = require('../../utils/util.js');

Page({
  data: {
    list: [],
    loading: true,
    page: 1,
    hasMore: true,
    isEmpty: false
  },

  onLoad() {
    this.loadList(true);
  },

  onShow() {
    // 从打卡页返回后刷新
    if (this.data.list.length) {
      this.loadList(true);
    }
  },

  async loadList(reset = false) {
    if (reset) this.setData({ page: 1, hasMore: true, list: [] });
    if (!this.data.hasMore && !reset) return;
    this.setData({ loading: true });
    try {
      const data = await api.getCheckinList({ page: this.data.page, pageSize: 20 });
      const newList = reset ? data.list : this.data.list.concat(data.list);
      this.setData({
        list: newList,
        hasMore: newList.length < data.total,
        page: reset ? 2 : this.data.page + 1,
        isEmpty: newList.length === 0
      });
    } catch (e) {
      console.error('load list error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  onReachBottom() {
    if (this.data.hasMore && !this.data.loading) {
      this.loadList(false);
    }
  },

  onPullDownRefresh() {
    this.loadList(true).finally(() => wx.stopPullDownRefresh());
  },

  formatTime(ts) {
    return util.timeAgo(ts);
  },

  async onDelete(e) {
    const id = e.currentTarget.dataset.id;
    const res = await new Promise(resolve => {
      wx.showModal({
        title: '确认删除',
        content: '删除后不可恢复，确定删除这条打卡吗？',
        success: r => resolve(r.confirm)
      });
    });
    if (!res) return;
    try {
      await api.deleteCheckin(id);
      const list = this.data.list.filter(i => i._id !== id);
      this.setData({ list, isEmpty: list.length === 0 });
      wx.showToast({ title: '已删除', icon: 'success' });
    } catch (e) {
      console.error('delete error', e);
    }
  }
});

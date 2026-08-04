// pages/class/class.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    role: '',
    classList: [],
    showCreate: false,
    className: '',
    creating: false,
    loading: true
  },

  onLoad() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    this.setData({ role: app.globalData.role });
    this.loadClasses();
  },

  onShow() {
    if (app.globalData.userInfo) this.loadClasses();
  },

  async loadClasses() {
    this.setData({ loading: true });
    try {
      const list = await api.getMyClasses();
      this.setData({ classList: list });
    } catch (e) {
      console.error('load classes error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  onNameInput(e) { this.setData({ className: e.detail.value }); },

  openCreate() { this.setData({ showCreate: true, className: '' }); },
  closeCreate() { this.setData({ showCreate: false }); },

  async onCreate() {
    const name = this.data.className.trim();
    if (!name) {
      wx.showToast({ title: '请填写班级名', icon: 'none' });
      return;
    }
    this.setData({ creating: true });
    try {
      await api.createClass({ name });
      wx.showToast({ title: '创建成功', icon: 'success' });
      this.setData({ showCreate: false, className: '' });
      this.loadClasses();
    } catch (e) {
      console.error('create class error', e);
    } finally {
      this.setData({ creating: false });
    }
  },

  goDetail(e) {
    const id = e.currentTarget.dataset.id;
    wx.navigateTo({ url: `/pages/class-detail/class-detail?id=${id}` });
  },

  copyCode(e) {
    const code = e.currentTarget.dataset.code;
    wx.setClipboardData({ data: code, success: () => wx.showToast({ title: '邀请码已复制', icon: 'success' }) });
  }
});

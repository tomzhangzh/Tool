// pages/class/class.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    role: '',
    classList: [],
    showCreate: false,
    showJoin: false,
    className: '',
    joinCode: '',
    creating: false,
    joining: false,
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
  onJoinCodeInput(e) { this.setData({ joinCode: e.detail.value }); },

  openCreate() { this.setData({ showCreate: true, className: '' }); },
  closeCreate() { this.setData({ showCreate: false }); },

  openJoin() { this.setData({ showJoin: true, joinCode: '' }); },
  closeJoin() { this.setData({ showJoin: false }); },

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

  async onJoin() {
    const code = (this.data.joinCode || '').trim().toUpperCase();
    if (!code) {
      wx.showToast({ title: '请填写邀请码', icon: 'none' });
      return;
    }
    this.setData({ joining: true });
    try {
      // 老师：凭邀请码成为共管老师；学生：加入班级
      const role = this.data.role;
      if (role === 'teacher') {
        const res = await api.addTeacher(code);
        wx.showToast({ title: `已加入「${res.name}」`, icon: 'success' });
      } else {
        await api.joinClass({ code });
        wx.showToast({ title: '加入成功', icon: 'success' });
      }
      this.setData({ showJoin: false, joinCode: '' });
      this.loadClasses();
    } catch (e) {
      console.error('join class error', e);
    } finally {
      this.setData({ joining: false });
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

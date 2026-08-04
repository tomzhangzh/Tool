// pages/profile/profile.js
const app = getApp();
const api = require('../../utils/api.js');
const { ROLE_LABEL } = require('../../utils/constants.js');

Page({
  data: {
    userInfo: null,
    roleLabel: '',
    // 概览统计
    summary: null,
    // 奖项数量
    awardCount: 0
  },

  onShow() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    this.setData({
      userInfo: app.globalData.userInfo,
      roleLabel: ROLE_LABEL[app.globalData.role] || ''
    });
    this.loadSummary();
  },

  async loadSummary() {
    try {
      const data = await api.getProfile();
      this.setData({ summary: data.summary, awardCount: data.awardCount || 0 });
    } catch (e) {
      console.error('load profile error', e);
    }
  },

  goAwards() {
    wx.navigateTo({ url: '/pages/awards/awards' });
  },

  goClass() {
    wx.navigateTo({ url: '/pages/class/class' });
  },

  goCheckinList() {
    wx.navigateTo({ url: '/pages/checkin-list/checkin-list' });
  },

  onChooseAvatar(e) {
    // 头像更新
    const avatar = e.detail.avatarUrl;
    api.updateProfile({ avatar }).then(() => {
      const u = app.globalData.userInfo;
      u.avatar = avatar;
      app.setLogin(u);
      this.setData({ userInfo: u });
      wx.showToast({ title: '已更新', icon: 'success' });
    });
  },

  onLogout() {
    wx.showModal({
      title: '退出登录',
      content: '确定退出当前账号吗？',
      success: r => {
        if (r.confirm) {
          app.logout();
          wx.redirectTo({ url: '/pages/login/login' });
        }
      }
    });
  }
});

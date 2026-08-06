// pages/class-detail/class-detail.js
const app = getApp();
const api = require('../../utils/api.js');

Page({
  data: {
    classId: '',
    info: null,
    members: [],
    teachers: [],
    loading: true,
    role: '',
    isCreator: false
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
      const [info, members, teachers] = await Promise.all([
        api.getClassDetail(this.data.classId),
        api.getClassMembers(this.data.classId),
        api.getClassTeachers(this.data.classId)
      ]);
      const isCreator = !!(info && app.globalData.openid && info.teacherOpenid === app.globalData.openid);
      this.setData({ info, members, teachers, isCreator });
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

  // 创建者移除共管老师
  onRemoveTeacher(e) {
    const targetOpenid = e.currentTarget.dataset.openid;
    const targetName = e.currentTarget.dataset.name || '该老师';
    wx.showModal({
      title: '移除老师',
      content: `确定将「${targetName}」移出班级管理？`,
      confirmColor: '#e64340',
      success: async (res) => {
        if (!res.confirm) return;
        try {
          await api.removeTeacher(this.data.classId, targetOpenid);
          wx.showToast({ title: '已移除', icon: 'success' });
          this.loadDetail();
        } catch (e) {
          console.error('remove teacher error', e);
        }
      }
    });
  },

  viewStats() {
    wx.navigateTo({ url: `/pages/stats/stats?classId=${this.data.classId}` });
  },

  viewRanking() {
    wx.navigateTo({ url: `/pages/ranking/ranking?classId=${this.data.classId}` });
  },

  // 分享班级，家长点击后可凭邀请码加入
  onShareAppMessage() {
    const info = this.data.info || {};
    return {
      title: `邀请你加入「${info.name || '运动班级'}」，邀请码：${info.code || ''}`,
      path: `/pages/index/index`,
      imageUrl: ''
    };
  },

  // 分享班级到朋友圈
  onShareTimeline() {
    const info = this.data.info || {};
    return {
      title: `运动班级「${info.name}」招募中，邀请码：${info.code}`,
      query: ''
    };
  }
});

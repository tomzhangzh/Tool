// pages/login/login.js
const app = getApp();
const api = require('../../utils/api.js');
const { ROLE_LABEL } = require('../../utils/constants.js');

Page({
  data: {
    step: 'role', // 'role' | 'info'
    role: '',
    roleLabel: '',
    name: '',
    avatar: '',
    weight: '',
    classCode: '', // 学生加入班级/家长绑定孩子用
    childName: '', // 家长填写孩子姓名
    loading: false
  },

  onLoad(options) {
    // 已登录直接跳首页
    if (app.globalData.userInfo) {
      this.goHome();
      return;
    }
    if (options.role) {
      this.setData({ role: options.role, roleLabel: ROLE_LABEL[options.role] || '', step: 'info' });
    }
  },

  // 选择角色
  onChooseRole(e) {
    const role = e.currentTarget.dataset.role;
    this.setData({ role, roleLabel: ROLE_LABEL[role], step: 'info' });
  },

  onNameInput(e) { this.setData({ name: e.detail.value }); },
  onWeightInput(e) {
    // 允许空字符串和正常数字输入，避免被强制覆盖回默认值
    const val = e.detail.value;
    this.setData({ weight: val });
  },
  onClassCodeInput(e) { this.setData({ classCode: e.detail.value.trim() }); },
  onChildNameInput(e) { this.setData({ childName: e.detail.value.trim() }); },

  // 选择头像（用微信头像）
  onChooseAvatar(e) {
    this.setData({ avatar: e.detail.avatarUrl });
  },

  back() {
    this.setData({ step: 'role', role: '', roleLabel: '' });
  },

  // 提交
  async onSubmit() {
    const { role, name, avatar, weight, classCode, childName } = this.data;
    if (!name || !name.trim()) {
      wx.showToast({ title: '请填写姓名', icon: 'none' });
      return;
    }
    if (role === 'student') {
      if (!weight || Number(weight) <= 0) {
        wx.showToast({ title: '请填写体重', icon: 'none' });
        return;
      }
      if (!classCode) {
        wx.showToast({ title: '请填写班级邀请码', icon: 'none' });
        return;
      }
    }
    if (role === 'parent' && !childName) {
      wx.showToast({ title: '请填写孩子姓名', icon: 'none' });
      return;
    }

    this.setData({ loading: true });
    try {
      // 先获取 openid
      const loginData = await api.login();
      const openid = loginData.openid;

      const payload = {
        openid,
        role,
        name: name.trim(),
        avatar: avatar || '',
        weight: role === 'student' ? Number(weight) : undefined,
        classCode: role === 'student' ? classCode : undefined,
        childName: role === 'parent' ? childName : undefined
      };
      const userInfo = await api.bindRole(payload);
      app.setLogin(userInfo);
      wx.showToast({ title: '欢迎加入', icon: 'success' });
      setTimeout(() => this.goHome(), 800);
    } catch (e) {
      console.error('login error', e);
    } finally {
      this.setData({ loading: false });
    }
  },

  goHome() {
    wx.switchTab({ url: '/pages/index/index' });
  }
});

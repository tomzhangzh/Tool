// pages/checkin/checkin.js
const app = getApp();
const api = require('../../utils/api.js');
const { EXERCISE_TYPES } = require('../../utils/constants.js');
const util = require('../../utils/util.js');

Page({
  data: {
    exerciseTypes: EXERCISE_TYPES,
    selectedType: null,
    selectedExercise: null,
    duration: 30,
    note: '',
    weight: 30,
    calorie: 0,
    submitting: false,
    // 今日已打卡
    todayList: []
  },

  onLoad() {
    if (!app.globalData.userInfo) {
      wx.redirectTo({ url: '/pages/login/login' });
      return;
    }
    if (app.globalData.role !== 'student') {
      wx.showToast({ title: '仅学生可打卡', icon: 'none' });
      setTimeout(() => wx.navigateBack(), 1000);
      return;
    }
    this.setData({ weight: app.globalData.userInfo.weight || 30 });
    this.loadToday();
  },

  async loadToday() {
    const list = await api.getTodayCheckin().catch(() => []);
    this.setData({ todayList: list });
  },

  onSelectType(e) {
    const type = e.currentTarget.dataset.type;
    const ex = EXERCISE_TYPES.find(t => t.id === type);
    this.setData({
      selectedType: type,
      selectedExercise: ex,
      calorie: util.calcCalorie(ex.met, this.data.duration, this.data.weight)
    });
  },

  onDurationChange(e) {
    const d = Number(e.detail.value) || 0;
    this.setData({
      duration: d,
      calorie: this.data.selectedExercise ? util.calcCalorie(this.data.selectedExercise.met, d, this.data.weight) : 0
    });
  },

  onNoteInput(e) {
    this.setData({ note: e.detail.value });
  },

  async onSubmit() {
    const { selectedExercise, duration, note } = this.data;
    if (!selectedExercise) {
      wx.showToast({ title: '请选择运动项目', icon: 'none' });
      return;
    }
    if (!duration || duration <= 0) {
      wx.showToast({ title: '请填写运动时长', icon: 'none' });
      return;
    }
    this.setData({ submitting: true });
    try {
      await api.submitCheckin({
        exerciseId: selectedExercise.id,
        exerciseName: selectedExercise.name,
        exerciseIcon: selectedExercise.icon,
        met: selectedExercise.met,
        duration,
        note
      });
      wx.showToast({ title: '打卡成功！', icon: 'success' });
      this.setData({
        selectedType: null,
        selectedExercise: null,
        duration: 30,
        note: '',
        calorie: 0
      });
      this.loadToday();
    } catch (e) {
      console.error('submit error', e);
    } finally {
      this.setData({ submitting: false });
    }
  },

  goList() {
    wx.navigateTo({ url: '/pages/checkin-list/checkin-list' });
  }
});

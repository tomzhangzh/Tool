// app.js
App({
  onLaunch() {
    if (!wx.cloud) {
      console.error('请使用 2.2.3 或以上的基础库以使用云能力');
    } else {
      wx.cloud.init({
        env: 'cloud1-d9g0cl0c6e5006db7', // 替换为你的云开发环境ID
        traceUser: true
      });
    }
    // 检查本地登录态
    this.checkLogin();
  },

  globalData: {
    userInfo: null,
    role: null, // 'teacher' | 'student' | 'parent'
    openid: null
  },

  checkLogin() {
    const userInfo = wx.getStorageSync('userInfo');
    if (userInfo) {
      this.globalData.userInfo = userInfo;
      this.globalData.role = userInfo.role;
      this.globalData.openid = userInfo.openid;
    }
  },

  setLogin(userInfo) {
    this.globalData.userInfo = userInfo;
    this.globalData.role = userInfo.role;
    this.globalData.openid = userInfo.openid;
    wx.setStorageSync('userInfo', userInfo);
  },

  logout() {
    this.globalData.userInfo = null;
    this.globalData.role = null;
    this.globalData.openid = null;
    wx.removeStorageSync('userInfo');
  }
});

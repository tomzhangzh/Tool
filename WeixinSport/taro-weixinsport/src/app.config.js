/**
 * Taro 全局配置
 */
export default {
  pages: [
    'pages/login/index',
    'pages/index/index',
    'pages/checkin/index',
    'pages/checkin-list/index',
    'pages/class-news/index',
    'pages/class/index',
    'pages/class-detail/index',
    'pages/weekly/index',
    'pages/monthly/index',
    'pages/ranking/index',
    'pages/stats/index',
    'pages/awards/index',
    'pages/profile/index'
  ],
  window: {
    navigationBarBackgroundColor: '#4A90E2',
    navigationBarTextStyle: 'white',
    navigationBarTitleText: '华曜运动打卡',
    backgroundTextStyle: 'light'
  },
  tabBar: {
    color: '#999999',
    selectedColor: '#4A90E2',
    backgroundColor: '#ffffff',
    borderStyle: 'black',
    list: [
      {
        pagePath: 'pages/index/index',
        text: '首页'
      },
      {
        pagePath: 'pages/checkin/index',
        text: '打卡'
      },
      {
        pagePath: 'pages/class/index',
        text: '班级'
      },
      {
        pagePath: 'pages/profile/index',
        text: '我的'
      }
    ]
  },
  mini: {
    postcss: {
      pxtransform: {
        enable: true,
        config: {
          selectorBlackList: ['nut-']
        }
      }
    }
  },
  h5: {
    publicPath: '/',
    staticDirectory: 'static',
    router: {
      mode: 'hash'
    },
    devServer: {
      port: 10086,
      disableHostCheck: true
    },
    // H5 环境使用自定义 TabBar，禁用原生 tabBar
    tabBar: false
  }
}

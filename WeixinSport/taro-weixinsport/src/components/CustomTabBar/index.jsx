import React from 'react';
import { View, Text } from '@tarojs/components';
import Taro, { useRouter } from '@tarojs/taro';
import './index.scss';

const tabs = [
  { pagePath: 'pages/index/index', text: '首页' },
  { pagePath: 'pages/checkin/index', text: '打卡' },
  { pagePath: 'pages/class-news/index', text: '班级动态' },
  { pagePath: 'pages/profile/index', text: '我的' }
];

export default function CustomTabBar() {
  const router = useRouter();
  const currentPath = router.path?.replace(/^\//, '') || '';

  const handleSwitch = (pagePath) => {
    const url = `/${pagePath}`;
    console.log('[CustomTabBar] switch to:', url);
    // 对于 tabBar 页面，使用 switchTab；否则使用 navigateTo
    if (['pages/index/index', 'pages/checkin/index', 'pages/profile/index'].includes(pagePath)) {
      Taro.switchTab({ 
        url,
        fail: (err) => {
          console.log('[CustomTabBar] switchTab fail:', err);
          // 降级使用 navigateTo
          Taro.navigateTo({ url });
        }
      });
    } else {
      Taro.navigateTo({ url });
    }
  };

  return (
    <View className='custom-tab-bar'>
      {tabs.map(tab => (
        <View
          key={tab.pagePath}
          className={`tab-item ${currentPath === tab.pagePath ? 'active' : ''}`}
          onClick={(e) => {
            e.stopPropagation();
            e.preventDefault();
            handleSwitch(tab.pagePath);
          }}
        >
          <Text className='tab-text'>{tab.text}</Text>
        </View>
      ))}
    </View>
  );
}

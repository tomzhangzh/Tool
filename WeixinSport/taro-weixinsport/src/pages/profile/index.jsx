// src/pages/profile/index.jsx
import React, { useState } from 'react';
import { View, Text, Button, Image, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole, setUserInfo, clearLogin } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Profile() {
  const [userInfo, setUserInfoState] = useState(null);
  const [roleLabel, setRoleLabel] = useState('');
  const [summary, setSummary] = useState(null);
  const [awardCount, setAwardCount] = useState(0);

  useDidShow(() => {
    const info = getUserInfo();
    const role = getRole();
    if (!info) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    setUserInfoState(info);
    setRoleLabel(ROLE_LABEL[role] || '');
    loadSummary();
  });

  const loadSummary = async () => {
    try {
      const data = await api.getProfile();
      setSummary(data.summary);
      setAwardCount(data.awardCount || 0);
    } catch (e) {
      console.error('load profile error', e);
    }
  };

  const handleAvatarUpdate = async (e) => {
    let avatarUrl = '';
    if (process.env.TARO_ENV === 'h5') {
      // H5 环境：模拟选择头像
      avatarUrl = 'https://api.dicebear.com/7.x/avataaars/svg?seed=' + Math.random();
    } else {
      avatarUrl = e.detail.avatarUrl;
    }
    
    try {
      await api.updateProfile({ avatar: avatarUrl });
      const updatedInfo = { ...userInfo, avatar: avatarUrl };
      setUserInfo(updatedInfo);
      setUserInfoState(updatedInfo);
      Taro.showToast({ title: '已更新', icon: 'success' });
    } catch (e) {
      console.error('update avatar error', e);
    }
  };

  const handleLogout = () => {
    Taro.showModal({
      title: '退出登录',
      content: '确定退出当前账号吗？',
      success: (res) => {
        if (res.confirm) {
          clearLogin();
          Taro.redirectTo({ url: '/pages/login/index' });
        }
      }
    });
  };

  return (
    <View className='profile-page page-with-tabbar'>
      <ScrollView scrollY className='content' style={{ paddingBottom: '80px' }}>
      {/* 用户信息头部 */}
      <View className='header'>
        <View className='avatar-wrapper'>
          {process.env.TARO_ENV === 'h5' ? (
            <View className='avatar' onClick={handleAvatarUpdate}>
              {userInfo?.avatar ? (
                <Image src={userInfo.avatar} className='avatar-img' />
              ) : (
                <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
              )}
            </View>
          ) : (
            <Button className='avatar-btn' openType='chooseAvatar' onChooseAvatar={handleAvatarUpdate}>
              <View className='avatar'>
                {userInfo?.avatar ? (
                  <Image src={userInfo.avatar} className='avatar-img' />
                ) : (
                  <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
                )}
              </View>
            </Button>
          )}
        </View>
        <Text className='user-name'>{userInfo?.name || '用户'}</Text>
        <Text className='user-role'>{roleLabel}</Text>
      </View>

      {/* 统计卡片 */}
      <View className='stats-card'>
        <View className='stats-item'>
          <Text className='stats-value'>{summary?.totalCheckins || 0}</Text>
          <Text className='stats-label'>总打卡次数</Text>
        </View>
        <View className='stats-item'>
          <Text className='stats-value'>{summary?.totalDuration || 0}</Text>
          <Text className='stats-label'>总时长(分)</Text>
        </View>
        <View className='stats-item'>
          <Text className='stats-value'>{awardCount}</Text>
          <Text className='stats-label'>获得奖项</Text>
        </View>
      </View>

      {/* 功能菜单 */}
      <View className='menu-list'>
        <View className='menu-item' onClick={() => Taro.navigateTo({ url: '/pages/awards/index' })}>
          <Text className='menu-icon'>🏆</Text>
          <Text className='menu-label'>我的奖项</Text>
          <Text className='menu-arrow'>›</Text>
        </View>
        <View className='menu-item' onClick={() => Taro.navigateTo({ url: '/pages/class/index' })}>
          <Text className='menu-icon'>📚</Text>
          <Text className='menu-label'>我的班级</Text>
          <Text className='menu-arrow'>›</Text>
        </View>
        <View className='menu-item' onClick={() => Taro.navigateTo({ url: '/pages/checkin-list/index' })}>
          <Text className='menu-icon'>📋</Text>
          <Text className='menu-label'>打卡记录</Text>
          <Text className='menu-arrow'>›</Text>
        </View>
      </View>

      {/* 退出按钮 */}
      <View className='logout-section'>
        <Button className='logout-btn' onClick={handleLogout}>退出登录</Button>
      </View>
      </ScrollView>
      {/* 自定义 TabBar */}
      <CustomTabBar />
    </View>
  );
}

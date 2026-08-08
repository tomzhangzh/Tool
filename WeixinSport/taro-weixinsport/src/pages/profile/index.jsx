// src/pages/profile/index.jsx
import React, { useState, useRef } from 'react';
import { View, Text, Button, Image, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole, setUserInfo, clearLogin } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import { callFunction } from '../../utils/cloud';
import { compressImageH5, blobToBase64 } from '../../utils/compress';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Profile() {
  const [userInfo, setUserInfoState] = useState(null);
  const [roleLabel, setRoleLabel] = useState('');
  const [summary, setSummary] = useState(null);
  const [awardCount, setAwardCount] = useState(0);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef(null);

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

  // H5 环境：选择头像文件
  const handleChooseAvatarH5 = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  // H5 环境：处理文件选择和压缩上传（通过云函数代理）
  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // 清空 input 值，允许重复选择同一文件
    e.target.value = '';

    try {
      setUploading(true);
      Taro.showLoading({ title: '压缩中...', mask: true });

      // 压缩图片到 1MB 以内
      const compressedBlob = await compressImageH5(file, 1024 * 1024);
      
      // 转换为 base64 用于云函数传输
      const base64 = await blobToBase64(compressedBlob);
      
      // 生成云端路径
      const ext = file.type.includes('png') ? 'png' : 'jpg';
      const ts = Date.now();
      const cloudPath = `avatars/avatar_${ts}.${ext}`;

      Taro.showLoading({ title: '上传中...', mask: true });

      // 通过 login 云函数的 uploadAvatar action 上传
      const result = await callFunction('login', {
        action: 'uploadAvatar',
        fileContent: base64,
        cloudPath,
      });

      if (result.result?.code !== 0) {
        throw new Error(result.result?.message || '上传失败');
      }

      const avatarUrl = result.result.data.url;

      // 更新用户信息
      await api.updateProfile({ avatar: avatarUrl });
      const updatedInfo = { ...userInfo, avatar: avatarUrl };
      setUserInfo(updatedInfo);
      setUserInfoState(updatedInfo);
      
      Taro.hideLoading();
      Taro.showToast({ title: '头像更新成功', icon: 'success' });
    } catch (err) {
      console.error('头像上传失败', err);
      Taro.hideLoading();
      Taro.showToast({ title: err.message || '上传失败，请重试', icon: 'none' });
    } finally {
      setUploading(false);
    }
  };

  // 小程序环境：选择头像
  const handleAvatarUpdateMini = async (e) => {
    const avatarUrl = e.detail.avatarUrl;
    if (!avatarUrl) return;

    try {
      setUploading(true);
      Taro.showLoading({ title: '上传中...', mask: true });

      // 小程序环境：直接上传临时文件到云存储
      const ext = 'jpg';
      const ts = Date.now();
      const cloudPath = `avatars/avatar_${ts}.${ext}`;

      const uploadResult = await new Promise((resolve, reject) => {
        Taro.cloud.uploadFile({
          cloudPath,
          filePath: avatarUrl,
          success: (res) => resolve({ url: res.fileID }),
          fail: reject
        });
      });

      // 更新用户信息
      await api.updateProfile({ avatar: uploadResult.url });
      const updatedInfo = { ...userInfo, avatar: uploadResult.url };
      setUserInfo(updatedInfo);
      setUserInfoState(updatedInfo);
      
      Taro.hideLoading();
      Taro.showToast({ title: '头像更新成功', icon: 'success' });
    } catch (err) {
      console.error('头像上传失败', err);
      Taro.hideLoading();
      Taro.showToast({ title: '上传失败，请重试', icon: 'none' });
    } finally {
      setUploading(false);
    }
  };

  const handleLogout = () => {
    Taro.showModal({
      title: '退出登录',
      content: '确定退出当前账号吗？',
      success: async (res) => {
        if (res.confirm) {
          await clearLogin();
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
            <View 
              className={`avatar ${uploading ? 'uploading' : ''}`} 
              onClick={handleChooseAvatarH5}
            >
              {userInfo?.avatar ? (
                <Image src={userInfo.avatar} className='avatar-img' mode='aspectFill' />
              ) : (
                <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
              )}
              {uploading && <View className='avatar-loading'><Text>上传中...</Text></View>}
            </View>
          ) : (
            <Button 
              className='avatar-btn' 
              openType='chooseAvatar' 
              onChooseAvatar={handleAvatarUpdateMini}
              disabled={uploading}
            >
              <View className={`avatar ${uploading ? 'uploading' : ''}`}>
                {userInfo?.avatar ? (
                  <Image src={userInfo.avatar} className='avatar-img' mode='aspectFill' />
                ) : (
                  <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
                )}
                {uploading && <View className='avatar-loading'><Text>上传中...</Text></View>}
              </View>
            </Button>
          )}
          {/* H5 隐藏的文件选择器 */}
          {process.env.TARO_ENV === 'h5' && (
            <input
              ref={fileInputRef}
              type='file'
              accept='image/*'
              style={{ display: 'none' }}
              onChange={handleFileChange}
            />
          )}
        </View>
        <Text className='user-name'>{userInfo?.name || '用户'}</Text>
        <Text className='user-role'>{roleLabel}</Text>
        {process.env.TARO_ENV === 'h5' && (
          <Text className='avatar-tip'>点击头像更换</Text>
        )}
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

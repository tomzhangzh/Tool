// src/pages/profile/index.jsx
import React, { useState, useRef, useEffect } from 'react';
import { View, Text, Button, Image, ScrollView, Input } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole, setUserInfo, clearLogin } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import { callFunction, resolveFileURL } from '../../utils/cloud';
import { compressImageH5, blobToBase64 } from '../../utils/compress';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Profile() {
  const [userInfo, setUserInfoState] = useState(null);
  const [roleLabel, setRoleLabel] = useState('');
  const [summary, setSummary] = useState(null);
  const [awardCount, setAwardCount] = useState(0);
  const [uploading, setUploading] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState(''); // 解析后的可访问 URL
  const fileInputRef = useRef(null);

  // 修改密码相关状态
  const [showPwdModal, setShowPwdModal] = useState(false);
  const [oldPwd, setOldPwd] = useState('');
  const [newPwd, setNewPwd] = useState('');
  const [confirmPwd, setConfirmPwd] = useState('');
  const [pwdLoading, setPwdLoading] = useState(false);

  // 退出班级相关状态
  const [showQuitModal, setShowQuitModal] = useState(false);
  const [classList, setClassList] = useState([]);
  const [quitLoading, setQuitLoading] = useState(false);

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

  // 当 userInfo 变化时，解析头像 fileID 为可访问的 URL
  useEffect(() => {
    const resolveAvatar = async () => {
      if (userInfo?.avatar) {
        const url = await resolveFileURL(userInfo.avatar);
        setAvatarUrl(url);
      } else {
        setAvatarUrl('');
      }
    };
    resolveAvatar();
  }, [userInfo?.avatar]);

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

    e.target.value = '';

    try {
      setUploading(true);
      Taro.showLoading({ title: '压缩中...', mask: true });

      const compressedBlob = await compressImageH5(file, 1024 * 1024);
      const base64 = await blobToBase64(compressedBlob);
      
      const ext = file.type.includes('png') ? 'png' : 'jpg';
      const ts = Date.now();
      const cloudPath = `avatars/avatar_${ts}.${ext}`;

      Taro.showLoading({ title: '上传中...', mask: true });

      const result = await callFunction('login', {
        action: 'uploadAvatar',
        fileContent: base64,
        cloudPath,
      });

      if (result.result?.code !== 0) {
        throw new Error(result.result?.message || '上传失败');
      }

      // 存储 fileID（永久有效）而不是临时 URL
      const fileID = result.result.data.fileID;

      await api.updateProfile({ avatar: fileID });
      const updatedInfo = { ...userInfo, avatar: fileID };
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

  // 打开修改密码弹窗
  const openPwdModal = () => {
    setOldPwd('');
    setNewPwd('');
    setConfirmPwd('');
    setShowPwdModal(true);
  };

  // 提交修改密码
  const handleChangePassword = async () => {
    if (!oldPwd) {
      Taro.showToast({ title: '请输入原密码', icon: 'none' });
      return;
    }
    if (!newPwd || newPwd.length < 4) {
      Taro.showToast({ title: '新密码至少4位', icon: 'none' });
      return;
    }
    if (newPwd !== confirmPwd) {
      Taro.showToast({ title: '两次密码不一致', icon: 'none' });
      return;
    }
    if (newPwd === oldPwd) {
      Taro.showToast({ title: '新密码不能与原密码相同', icon: 'none' });
      return;
    }

    try {
      setPwdLoading(true);
      await api.changePassword({ oldPassword: oldPwd, newPassword: newPwd });
      setShowPwdModal(false);
      Taro.showToast({ title: '密码修改成功', icon: 'success' });
    } catch (e) {
      // api.js 已处理 toast
    } finally {
      setPwdLoading(false);
    }
  };

  // 打开退出班级弹窗，加载班级列表
  const openQuitModal = async () => {
    try {
      setQuitLoading(true);
      const data = await api.getMyClasses(userInfo?.username);
      setClassList(data || []);
      setShowQuitModal(true);
    } catch (e) {
      console.error(e);
    } finally {
      setQuitLoading(false);
    }
  };

  // 确认退出班级
  const handleQuitClass = (cls) => {
    Taro.showModal({
      title: '退出班级',
      content: `确定要退出"${cls.name}"吗？`,
      success: async (res) => {
        if (res.confirm) {
          try {
            setQuitLoading(true);
            await api.quitClass(cls._id);
            Taro.showToast({ title: '已退出班级', icon: 'success' });
            // 刷新班级列表
            const data = await api.getMyClasses(userInfo?.username);
            setClassList(data || []);
            // 如果退出后没有班级了，关闭弹窗
            if (!data || data.length === 0) {
              setShowQuitModal(false);
            }
            // 刷新 profile summary
            loadSummary();
          } catch (e) {
            // api.js 已处理 toast
          } finally {
            setQuitLoading(false);
          }
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
              {avatarUrl ? (
                <Image src={avatarUrl} className='avatar-img' mode='aspectFill' />
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
                {avatarUrl ? (
                  <Image src={avatarUrl} className='avatar-img' mode='aspectFill' />
                ) : (
                  <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
                )}
                {uploading && <View className='avatar-loading'><Text>上传中...</Text></View>}
              </View>
            </Button>
          )}
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
        <View className='menu-item' onClick={openPwdModal}>
          <Text className='menu-icon'>🔒</Text>
          <Text className='menu-label'>修改密码</Text>
          <Text className='menu-arrow'>›</Text>
        </View>
        <View className='menu-item' onClick={openQuitModal}>
          <Text className='menu-icon'>🚪</Text>
          <Text className='menu-label'>退出班级</Text>
          <Text className='menu-arrow'>›</Text>
        </View>
      </View>

      {/* 退出按钮 */}
      <View className='logout-section'>
        <Button className='logout-btn' onClick={handleLogout}>退出登录</Button>
      </View>
      </ScrollView>

      {/* 修改密码弹窗 */}
      {showPwdModal && (
        <View className='modal-mask' onClick={() => !pwdLoading && setShowPwdModal(false)}>
          <View className='modal-box' onClick={(e) => e.stopPropagation()}>
            <Text className='modal-title'>修改密码</Text>
            <View className='modal-form'>
              <View className='form-row'>
                <Text className='form-label'>原密码</Text>
                <Input 
                  className='form-input' 
                  type='password' 
                  placeholder='请输入原密码' 
                  value={oldPwd} 
                  onInput={(e) => setOldPwd(e.detail.value)} 
                />
              </View>
              <View className='form-row'>
                <Text className='form-label'>新密码</Text>
                <Input 
                  className='form-input' 
                  type='password' 
                  placeholder='至少4位字符' 
                  value={newPwd} 
                  onInput={(e) => setNewPwd(e.detail.value)} 
                />
              </View>
              <View className='form-row'>
                <Text className='form-label'>确认密码</Text>
                <Input 
                  className='form-input' 
                  type='password' 
                  placeholder='再次输入新密码' 
                  value={confirmPwd} 
                  onInput={(e) => setConfirmPwd(e.detail.value)} 
                />
              </View>
            </View>
            <View className='modal-actions'>
              <Button className='modal-btn cancel' onClick={() => setShowPwdModal(false)} disabled={pwdLoading}>取消</Button>
              <Button className='modal-btn confirm' onClick={handleChangePassword} loading={pwdLoading}>确认修改</Button>
            </View>
          </View>
        </View>
      )}

      {/* 退出班级弹窗 */}
      {showQuitModal && (
        <View className='modal-mask' onClick={() => !quitLoading && setShowQuitModal(false)}>
          <View className='modal-box quit-modal' onClick={(e) => e.stopPropagation()}>
            <Text className='modal-title'>退出班级</Text>
            {quitLoading ? (
              <View className='modal-empty'><Text>加载中...</Text></View>
            ) : classList.length === 0 ? (
              <View className='modal-empty'>
                <Text className='empty-text'>暂无加入的班级</Text>
              </View>
            ) : (
              <ScrollView scrollY className='class-list'>
                {classList.map((cls) => (
                  <View key={cls._id} className='class-item'>
                    <View className='class-info'>
                      <Text className='class-name'>{cls.name}</Text>
                      <Text className='class-meta'>
                        {cls.isTeacher ? '老师' : '学生'} · {cls.memberCount || 0}人
                      </Text>
                    </View>
                    <Button 
                      className='quit-btn' 
                      size='mini' 
                      onClick={() => handleQuitClass(cls)}
                    >
                      退出
                    </Button>
                  </View>
                ))}
              </ScrollView>
            )}
            <View className='modal-actions'>
              <Button className='modal-btn cancel' onClick={() => setShowQuitModal(false)}>关闭</Button>
            </View>
          </View>
        </View>
      )}

      {/* 自定义 TabBar */}
      <CustomTabBar />
    </View>
  );
}

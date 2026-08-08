// src/pages/login/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text, Input, Button, Image } from '@tarojs/components';
import Taro from '@tarojs/taro';
import api from '../../utils/api';
import { setUserInfo, handleWxCallback, redirectToWxAuth, getOpenid } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import './index.scss';

export default function Login() {
  const [step, setStep] = useState('role'); // 'role' | 'info'
  const [role, setRole] = useState('');
  const [roleLabel, setRoleLabel] = useState('');
  const [name, setName] = useState('');
  const [avatar, setAvatar] = useState('');
  const [weight, setWeight] = useState('');
  const [classCode, setClassCode] = useState('');
  const [childName, setChildName] = useState('');
  const [loading, setLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  useEffect(() => {
    // 检查是否已登录
    const openid = getOpenid();
    if (openid) {
      Taro.switchTab({ url: '/pages/index/index' });
    } else {
      // H5 环境下处理微信回调
      handleWxCallback().then(userInfo => {
        if (userInfo) {
          Taro.switchTab({ url: '/pages/index/index' });
        }
      });
    }
  }, []);

  const onChooseRole = (selectedRole) => {
    setRole(selectedRole);
    setRoleLabel(ROLE_LABEL[selectedRole] || '');
    setStep('info');
  };

  const handleSubmit = async () => {
    if (!name || !name.trim()) {
      Taro.showToast({ title: '请填写姓名', icon: 'none' });
      return;
    }
    if (role === 'student') {
      if (!weight || Number(weight) <= 0) {
        Taro.showToast({ title: '请填写体重', icon: 'none' });
        return;
      }
      if (!classCode) {
        Taro.showToast({ title: '请填写班级邀请码', icon: 'none' });
        return;
      }
    }
    if (role === 'parent' && !childName) {
      Taro.showToast({ title: '请填写孩子姓名', icon: 'none' });
      return;
    }

    setLoading(true);
    setLoginError('');
    try {
      // 先尝试登录获取 openid（小程序环境会直接返回，H5 环境如果是首次可能需要先授权）
      let userInfo = await api.login();

      // 如果 H5 环境下没有 openid，说明需要先授权
      if (!userInfo || !userInfo.openid) {
        if (process.env.TARO_ENV === 'h5') {
          const redirectUri = window.location.href;
          redirectToWxAuth(redirectUri);
          return; // 授权后会回调回来
        } else {
          throw new Error('获取用户信息失败');
        }
      }

      // 绑定角色
      const payload = {
        role,
        name: name.trim(),
        avatar: avatar || '',
        weight: role === 'student' ? Number(weight) : undefined,
        classCode: role === 'student' ? classCode : undefined,
        childName: role === 'parent' ? childName : undefined
      };
      userInfo = await api.bindRole(payload);
      setUserInfo(userInfo);
      Taro.showToast({ title: '欢迎加入', icon: 'success' });
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 800);
    } catch (e) {
      console.error('login error', e);
      // 显示错误信息并允许重试
      setLoginError(e.message || '登录失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View className='login-page'>
      <View className='brand'>
        <View className='brand-icon'>🏃‍♂️</View>
        <View className='brand-title'>运动小达人</View>
        <View className='brand-sub'>运动打卡 · 周评选 · 月度明星</View>
      </View>

      {step === 'role' && (
        <View className='card'>
          <View className='card-title'>选择你的身份</View>
          <View className='role-list'>
            <View className='role-item' onClick={() => onChooseRole('student')}>
              <View className='role-emoji'>🧒</View>
              <View className='role-info'>
                <View className='role-name'>我是学生</View>
                <View className='role-desc'>每天打卡运动，争取拿奖</View>
              </View>
              <View className='role-arrow'>›</View>
            </View>
            <View className='role-item' onClick={() => onChooseRole('teacher')}>
              <View className='role-emoji'>👩‍🏫</View>
              <View className='role-info'>
                <View className='role-name'>我是老师</View>
                <View className='role-desc'>管理班级、查看统计、评选明星</View>
              </View>
              <View className='role-arrow'>›</View>
            </View>
            <View className='role-item' onClick={() => onChooseRole('parent')}>
              <View className='role-emoji'>👨‍👩‍👧</View>
              <View className='role-info'>
                <View className='role-name'>我是家长</View>
                <View className='role-desc'>关注孩子运动表现，鼓励陪伴</View>
              </View>
              <View className='role-arrow'>›</View>
            </View>
          </View>
        </View>
      )}

      {step === 'info' && (
        <View className='card'>
          <View className='card-title'>完善信息（{roleLabel}）</View>

          <View className='form-row'>
            <Text className='form-label'>头像</Text>
            {/* H5 环境：可点击生成随机头像 */}
            {process.env.TARO_ENV === 'h5' ? (
              <div className='avatar-btn' onClick={() => {
                setAvatar('https://api.dicebear.com/7.x/avataaars/svg?seed=' + Math.random());
              }}>
                {avatar ? <Image src={avatar} className='avatar' /> : <View className='avatar avatar-placeholder'>点击选择</View>}
              </div>
            ) : (
              <Button className='avatar-btn' openType='chooseAvatar' onChooseAvatar={(e) => setAvatar(e.detail.avatarUrl)}>
                {avatar ? <Image src={avatar} className='avatar' /> : <View className='avatar avatar-placeholder'>点击选择</View>}
              </Button>
            )}
          </View>

          <View className='form-row'>
            <Text className='form-label'>{role === 'parent' ? '家长姓名' : '姓名'}</Text>
            <Input 
              className='form-input' 
              placeholder='请输入姓名' 
              value={name} 
              onInput={(e) => setName(e.detail.value)} 
            />
          </View>

          {role === 'student' && (
            <>
              <View className='form-row'>
                <Text className='form-label'>体重(kg)</Text>
                <Input className='form-input' type='number' placeholder='用于计算卡路里（如30）' value={weight} onInput={(e) => setWeight(e.detail.value)} />
              </View>
              <View className='form-row'>
                <Text className='form-label'>班级邀请码</Text>
                <Input className='form-input' placeholder='请输入老师提供的邀请码' value={classCode} onInput={(e) => setClassCode(e.detail.value)} />
              </View>
            </>
          )}

          {role === 'parent' && (
            <View className='form-row'>
              <Text className='form-label'>孩子姓名</Text>
              <Input className='form-input' placeholder='绑定的孩子姓名' value={childName} onInput={(e) => setChildName(e.detail.value)} />
            </View>
          )}

          {loginError && (
            <View className='error-tip'>
              <Text>{loginError}</Text>
              <Text className='error-retry' onClick={handleSubmit}>点击重试</Text>
            </View>
          )}

          <View className='form-actions'>
            <Button className='btn btn-ghost' onClick={() => setStep('role')}>返回</Button>
            <Button className='btn btn-block' onClick={handleSubmit} loading={loading} disabled={loading}>
              完成注册
            </Button>
          </View>
        </View>
      )}
    </View>
  );
}

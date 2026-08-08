// src/pages/login/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text, Input, Button, Image } from '@tarojs/components';
import Taro from '@tarojs/taro';
import api from '../../utils/api';
import { setUserInfo, getUserInfo, getUsername, clearLogin } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import './index.scss';

export default function Login() {
  // 登录模式：'account' | 'register' | 'role' | 'info'
  const [mode, setMode] = useState('account');
  const [step, setStep] = useState('role');
  const [role, setRole] = useState('');
  const [roleLabel, setRoleLabel] = useState('');
  const [name, setName] = useState('');
  const [avatar, setAvatar] = useState('');
  const [weight, setWeight] = useState('');
  const [classCode, setClassCode] = useState('');
  const [teacherCode, setTeacherCode] = useState('');
  const [childName, setChildName] = useState('');
  
  // 账号密码字段
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  
  const [loading, setLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  useEffect(() => {
    // 检查是否已登录
    const userInfo = getUserInfo();
    if (userInfo && getUsername()) {
      Taro.switchTab({ url: '/pages/index/index' });
    }
  }, []);

  const onChooseRole = (selectedRole) => {
    setRole(selectedRole);
    setRoleLabel(ROLE_LABEL[selectedRole] || '');
    setMode('register');
    setStep('info');
  };

  // ========== 账号密码登录 ==========
  const handleAccountLogin = async () => {
    if (!username || !username.trim()) {
      Taro.showToast({ title: '请输入用户名', icon: 'none' });
      return;
    }
    if (!password) {
      Taro.showToast({ title: '请输入密码', icon: 'none' });
      return;
    }

    setLoading(true);
    setLoginError('');
    try {
      // 先确保 CloudBase 已登录（获取 _uid）
      await api.login();
      
      // 使用账号密码登录
      const userInfo = await api.loginByAccount({ username: username.trim(), password });
      
      console.log('[login] 登录返回用户信息:', userInfo);
      
      // 验证 role 字段
      if (!userInfo.role) {
        console.warn('[login] 用户信息中缺少 role 字段');
      }
      
      setUserInfo(userInfo);
      Taro.showToast({ title: '登录成功', icon: 'success' });
      // 延迟跳转，确保软键盘完全收起
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 1500);
    } catch (e) {
      console.error('login error', e);
      setLoginError(e.message || '登录失败，请检查用户名和密码');
    } finally {
      setLoading(false);
    }
  };

  // ========== 注册新用户 ==========
  const handleRegister = async () => {
    if (!username || !username.trim()) {
      Taro.showToast({ title: '请输入用户名', icon: 'none' });
      return;
    }
    if (!password || password.length < 6) {
      Taro.showToast({ title: '密码至少6位', icon: 'none' });
      return;
    }
    if (password !== confirmPassword) {
      Taro.showToast({ title: '两次密码不一致', icon: 'none' });
      return;
    }

    // 学生必须输入班级邀请码
    if (role === 'student' && (!classCode || !classCode.trim())) {
      Taro.showToast({ title: '请输入班级邀请码', icon: 'none' });
      return;
    }

    // 老师必须输入注册码
    if (role === 'teacher' && (!teacherCode || !teacherCode.trim())) {
      Taro.showToast({ title: '请输入老师注册码', icon: 'none' });
      return;
    }

    // 家长必须输入孩子姓名
    if (role === 'parent' && (!childName || !childName.trim())) {
      Taro.showToast({ title: '请输入孩子姓名', icon: 'none' });
      return;
    }

    setLoading(true);
    setLoginError('');
    try {
      // 先确保 CloudBase 已登录
      await api.login();
      
      // 注册新用户
      const userInfo = await api.register({
        username: username.trim(),
        password,
        role,
        name: name.trim() || username.trim(),
        avatar: avatar || '',
        weight: role === 'student' ? Number(weight) : undefined,
        classCode: role === 'student' ? classCode.trim() : undefined,
        teacherCode: role === 'teacher' ? teacherCode.trim() : undefined,
        childName: role === 'parent' ? childName.trim() : undefined
      });
      
      setUserInfo(userInfo);
      Taro.showToast({ title: '注册成功', icon: 'success' });
      // 延迟跳转，确保软键盘完全收起
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 1500);
    } catch (e) {
      console.error('register error', e);
      setLoginError(e.message || '注册失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  // ========== 微信登录（小程序端 / 旧版）==========
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
      // 先确保 CloudBase 已登录
      await api.login();

      // 绑定角色
      const payload = {
        role,
        name: name.trim(),
        avatar: avatar || '',
        weight: role === 'student' ? Number(weight) : undefined,
        classCode: role === 'student' ? classCode : undefined,
        childName: role === 'parent' ? childName : undefined
      };
      const userInfo = await api.bindRole(payload);
      setUserInfo(userInfo);
      Taro.showToast({ title: '欢迎加入', icon: 'success' });
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 800);
    } catch (e) {
      console.error('login error', e);
      setLoginError(e.message || '登录失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  // ========== 渲染 ==========
  
  // 账号登录/注册页面
  if (mode === 'account') {
    return (
      <View className='login-page'>
        <View className='brand'>
          <View className='brand-icon'>🏃‍♂️</View>
          <View className='brand-title'>华曜运动小达人</View>
          <View className='brand-sub'>运动打卡 · 周评选 · 月度明星</View>
        </View>

        <View className='card'>
          <View className='card-title'>账号登录</View>
          
          <View className='form-row'>
            <Text className='form-label'>用户名</Text>
            <Input 
              className='form-input' 
              placeholder='请输入用户名' 
              value={username} 
              onInput={(e) => setUsername(e.detail.value)} 
            />
          </View>

          <View className='form-row'>
            <Text className='form-label'>密码</Text>
            <View className='password-input-wrapper'>
              <Input 
                className='form-input password-input' 
                type={showPassword ? 'text' : 'password'}
                placeholder='请输入密码' 
                value={password} 
                onInput={(e) => setPassword(e.detail.value)} 
              />
              <View className='password-eye' onClick={() => setShowPassword(!showPassword)}>
                <Text>{showPassword ? '👁️‍🗨️' : '👁️'}</Text>
              </View>
            </View>
          </View>

          {loginError && (
            <View className='error-tip'>
              <Text>{loginError}</Text>
              <Text className='error-retry' onClick={handleAccountLogin}>点击重试</Text>
            </View>
          )}

          <View className='form-actions'>
            <Button className='btn btn-block' onClick={handleAccountLogin} loading={loading} disabled={loading}>
              登录
            </Button>
          </View>

          <View className='switch-tip'>
            <Text className='switch-text'>还没有账号？</Text>
            <Text className='switch-link' onClick={() => {
              setUsername('');
              setPassword('');
              setConfirmPassword('');
              setName('');
              setAvatar('');
              setWeight('');
              setClassCode('');
              setTeacherCode('');
              setChildName('');
              setLoginError('');
              setShowPassword(false);
              setShowConfirmPassword(false);
              setMode('role');
              setStep('role');
            }}>立即注册</Text>
          </View>
        </View>
      </View>
    );
  }

  // 注册 - 选择角色
  if (mode === 'role') {
    return (
      <View className='login-page'>
        <View className='brand'>
          <View className='brand-icon'>🏃‍♂️</View>
          <View className='brand-title'>华曜运动小达人</View>
          <View className='brand-sub'>运动打卡 · 周评选 · 月度明星</View>
        </View>

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

          <View className='form-actions' style={{ marginTop: '16px' }}>
            <Button className='btn btn-ghost' onClick={() => setMode('account')}>返回登录</Button>
          </View>
        </View>
      </View>
    );
  }

  // 注册 - 填写信息
  if (mode === 'register' || step === 'info') {
    return (
      <View className='login-page'>
        <View className='brand'>
          <View className='brand-icon'>🏃‍♂️</View>
          <View className='brand-title'>华曜运动小达人</View>
          <View className='brand-sub'>运动打卡 · 周评选 · 月度明星</View>
        </View>

        <View className='card'>
          <View className='card-title'>注册账号（{roleLabel}）</View>

          <View className='form-row'>
            <Text className='form-label'>用户名</Text>
            <Input 
              className='form-input' 
              placeholder='设置登录用户名' 
              value={username} 
              onInput={(e) => {
                const val = e.detail?.value || e.target?.value || '';
                setUsername(val);
              }}
            />
          </View>

          <View className='form-row'>
            <Text className='form-label'>密码</Text>
            <View className='password-input-wrapper'>
              <Input 
                className='form-input password-input' 
                type={showPassword ? 'text' : 'password'}
                placeholder='至少6位' 
                value={password} 
                onInput={(e) => {
                  const val = e.detail?.value || e.target?.value || '';
                  setPassword(val);
                }}
              />
              <View className='password-eye' onClick={() => setShowPassword(!showPassword)}>
                <Text>{showPassword ? '👁️‍🗨️' : '👁️'}</Text>
              </View>
            </View>
          </View>

          <View className='form-row'>
            <Text className='form-label'>确认密码</Text>
            <View className='password-input-wrapper'>
              <Input 
                className='form-input password-input' 
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder='再次输入密码' 
                value={confirmPassword} 
                onInput={(e) => {
                  const val = e.detail?.value || e.target?.value || '';
                  setConfirmPassword(val);
                }}
              />
              <View className='password-eye' onClick={() => setShowConfirmPassword(!showConfirmPassword)}>
                <Text>{showConfirmPassword ? '👁️‍🗨️' : '👁️'}</Text>
              </View>
            </View>
          </View>

          <View className='form-row'>
            <Text className='form-label'>{role === 'parent' ? '家长姓名' : '姓名'}</Text>
            <Input 
              className='form-input' 
              placeholder='请输入真实姓名' 
              value={name} 
              onInput={(e) => setName(e.detail.value)} 
            />
          </View>

          <View className='form-row'>
            <Text className='form-label'>头像</Text>
            <div className='avatar-btn' onClick={() => {
              setAvatar('https://api.dicebear.com/7.x/avataaars/svg?seed=' + Math.random());
            }}>
              {avatar ? <Image src={avatar} className='avatar' /> : <View className='avatar avatar-placeholder'>点击生成</View>}
            </div>
          </View>

          {role === 'student' && (
            <>
              <View className='form-row'>
                <Text className='form-label'>体重(kg)</Text>
                <Input className='form-input' type='number' placeholder='用于计算卡路里（如30）' value={weight} onInput={(e) => setWeight(e.detail.value)} />
              </View>
              <View className='form-row'>
                <Text className='form-label'>班级邀请码</Text>
                <Input className='form-input' placeholder='请输入老师提供的邀请码（必填）' value={classCode} onInput={(e) => setClassCode(e.detail.value)} />
              </View>
            </>
          )}

          {role === 'teacher' && (
            <View className='form-row'>
              <Text className='form-label'>老师注册码</Text>
              <Input className='form-input' placeholder='请输入管理员提供的注册码（必填）' value={teacherCode} onInput={(e) => setTeacherCode(e.detail.value)} />
            </View>
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
            </View>
          )}

          <View className='form-actions'>
            <Button className='btn btn-ghost' onClick={() => {
              setStep('role');
              setMode('role');
              setUsername('');
              setPassword('');
              setConfirmPassword('');
              setName('');
              setAvatar('');
              setWeight('');
              setClassCode('');
              setTeacherCode('');
              setChildName('');
              setShowPassword(false);
              setShowConfirmPassword(false);
            }}>返回</Button>
            <Button className='btn btn-block' onClick={handleRegister} loading={loading} disabled={loading}>
              完成注册
            </Button>
          </View>

          <View className='switch-tip'>
            <Text className='switch-text'>已有账号？</Text>
            <Text className='switch-link' onClick={() => {
              setMode('account');
              setUsername('');
              setPassword('');
              setConfirmPassword('');
              setName('');
              setAvatar('');
              setWeight('');
              setClassCode('');
              setTeacherCode('');
              setChildName('');
              setShowPassword(false);
              setShowConfirmPassword(false);
            }}>去登录</Text>
          </View>
        </View>
      </View>
    );
  }

  return null;
}

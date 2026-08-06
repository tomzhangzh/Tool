// src/pages/class/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text, Input, Button, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Class() {
  const [role, setRole] = useState('');
  const [classList, setClassList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [showJoin, setShowJoin] = useState(false);
  const [className, setClassName] = useState('');
  const [joinCode, setJoinCode] = useState('');
  const [creating, setCreating] = useState(false);
  const [joining, setJoining] = useState(false);

  useDidShow(() => {
    const userInfo = getUserInfo();
    const currentRole = getRole();
    if (!userInfo) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    setRole(currentRole);
    loadClasses();
  });

  const loadClasses = async () => {
    setLoading(true);
    try {
      const list = await api.getMyClasses();
      setClassList(list);
    } catch (e) {
      console.error('load classes error', e);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    const name = className.trim();
    if (!name) {
      Taro.showToast({ title: '请填写班级名', icon: 'none' });
      return;
    }
    setCreating(true);
    try {
      await api.createClass({ name });
      Taro.showToast({ title: '创建成功', icon: 'success' });
      setShowCreate(false);
      setClassName('');
      loadClasses();
    } catch (e) {
      console.error('create class error', e);
    } finally {
      setCreating(false);
    }
  };

  const handleJoin = async () => {
    const code = (joinCode || '').trim().toUpperCase();
    if (!code) {
      Taro.showToast({ title: '请填写邀请码', icon: 'none' });
      return;
    }
    setJoining(true);
    try {
      if (role === 'teacher') {
        const res = await api.addTeacher(code);
        Taro.showToast({ title: `已加入「${res.name}」`, icon: 'success' });
      } else {
        await api.joinClass({ code });
        Taro.showToast({ title: '加入成功', icon: 'success' });
      }
      setShowJoin(false);
      setJoinCode('');
      loadClasses();
    } catch (e) {
      console.error('join class error', e);
    } finally {
      setJoining(false);
    }
  };

  const goDetail = (id) => {
    Taro.navigateTo({ url: `/pages/class-detail/index?id=${id}` });
  };

  const copyCode = (code) => {
    Taro.setClipboardData({
      data: code,
      success: () => Taro.showToast({ title: '邀请码已复制', icon: 'success' })
    });
  };

  return (
    <View className='class-page page-with-tabbar'>
      <ScrollView scrollY className='content' style={{ paddingBottom: '80px' }}>
      {/* 老师视图：创建/加入入口 */}
      {role === 'teacher' && (
        <>
          <View className='card create-card' onClick={() => setShowCreate(true)}>
            <Text className='create-emoji'>➕</Text>
            <View className='create-text'>
              <Text className='create-title'>创建班级</Text>
              <Text className='create-desc'>为你的学生创建一个运动班级</Text>
            </View>
          </View>

          <View className='card join-card' onClick={() => setShowJoin(true)}>
            <Text className='create-emoji'>🤝</Text>
            <View className='create-text'>
              <Text className='create-title'>加入班级</Text>
              <Text className='create-desc'>凭邀请码成为共管老师</Text>
            </View>
          </View>
        </>
      )}

      {/* 学生/家长视图：加入入口 */}
      {role !== 'teacher' && (
        <View className='card join-card' onClick={() => setShowJoin(true)}>
          <Text className='create-emoji'>🎒</Text>
          <View className='create-text'>
            <Text className='create-title'>加入班级</Text>
            <Text className='create-desc'>输入老师提供的邀请码加入</Text>
          </View>
        </View>
      )}

      {/* 班级列表 */}
      {loading ? (
        <View className='loading'>加载中...</View>
      ) : classList.length > 0 ? (
        <View className='class-list'>
          {classList.map(cls => (
            <View 
              className='card class-card' 
              key={cls._id} 
              onClick={() => goDetail(cls._id)}
            >
              <View className='class-card-header'>
                <Text className='class-card-icon'>📚</Text>
                <View className='class-card-info'>
                  <Text className='class-card-name'>{cls.name}</Text>
                  <Text className='class-card-desc'>
                    {cls.memberCount || 0} 名学生 
                    {cls.teacherCount > 1 && ` · ${cls.teacherCount} 位老师`}
                  </Text>
                </View>
                <Text className='class-card-arrow'>›</Text>
              </View>
              {cls.isTeacher && (
                <View className='code-box' onClick={(e) => { e.stopPropagation(); copyCode(cls.code); }}>
                  <Text className='code-label'>邀请码</Text>
                  <Text className='code-value'>{cls.code}</Text>
                  <Text className='copy-btn'>复制</Text>
                </View>
              )}
            </View>
          ))}
        </View>
      ) : (
        <View className='empty'>
          <Text className='empty-emoji'>📚</Text>
          <Text className='empty-text'>还没有班级，点击上方创建或加入</Text>
        </View>
      )}

      {/* 创建弹窗 */}
      {showCreate && (
        <View className='modal-mask' onClick={() => setShowCreate(false)}>
          <View className='modal' onClick={(e) => e.stopPropagation()}>
            <Text className='modal-title'>创建班级</Text>
            <Input 
              className='modal-input' 
              placeholder='如：三年2班' 
              value={className}
              onInput={(e) => setClassName(e.detail.value)}
            />
            <View className='modal-actions'>
              <Button className='btn btn-ghost' onClick={() => setShowCreate(false)}>取消</Button>
              <Button className='btn btn-primary' onClick={handleCreate} loading={creating} disabled={creating}>
                确定
              </Button>
            </View>
          </View>
        </View>
      )}

      {/* 加入弹窗 */}
      {showJoin && (
        <View className='modal-mask' onClick={() => setShowJoin(false)}>
          <View className='modal' onClick={(e) => e.stopPropagation()}>
            <Text className='modal-title'>
              {role === 'teacher' ? '加入班级成为共管老师' : '加入班级'}
            </Text>
            <Input 
              className='modal-input' 
              placeholder='请输入6位邀请码' 
              value={joinCode}
              onInput={(e) => setJoinCode(e.detail.value)}
              maxLength={6}
            />
            <View className='modal-actions'>
              <Button className='btn btn-ghost' onClick={() => setShowJoin(false)}>取消</Button>
              <Button className='btn btn-primary' onClick={handleJoin} loading={joining} disabled={joining}>
                确定
              </Button>
            </View>
          </View>
        </View>
      )}
      </ScrollView>
      {/* 自定义 TabBar */}
      <CustomTabBar />
    </View>
  );
}

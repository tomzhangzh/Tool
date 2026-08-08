// src/pages/index/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text, ScrollView, Image } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole, clearLogin } from '../../utils/auth';
import { ROLE_LABEL } from '../../utils/constants';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Index() {
  const [userInfo, setUserInfo] = useState(null);
  const [role, setRole] = useState('');
  const [loading, setLoading] = useState(true);
  const [todayCheckin, setTodayCheckin] = useState([]);
  const [weekStats, setWeekStats] = useState(null);
  const [classList, setClassList] = useState([]);

  useDidShow(() => {
    loadData();
  });

  const loadData = async () => {
    const info = getUserInfo();
    const currentRole = getRole();
    if (!info) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    setUserInfo(info);
    setRole(currentRole);
    setLoading(true);

    try {
      if (currentRole === 'student') {
        const [today, week] = await Promise.all([
          api.getTodayCheckin().catch(() => []),
          api.getWeeklyStats({}).catch(() => null)
        ]);
        setTodayCheckin(today);
        setWeekStats(week);
      } else if (currentRole === 'teacher') {
        const list = await api.getMyClasses().catch(() => []);
        setClassList(list);
      } else if (currentRole === 'parent') {
        // 家长：加载孩子的班级列表和统计数据
        const [list, stats] = await Promise.all([
          api.getMyClasses().catch(() => []),
          api.getProfile().catch(() => null)
        ]);
        setClassList(list);
        setWeekStats(stats);
      }
    } catch (e) {
      console.error('load data error', e);
    } finally {
      setLoading(false);
    }
  };

  const goCheckin = () => Taro.navigateTo({ url: '/pages/checkin/index' });
  const goClass = () => Taro.navigateTo({ url: '/pages/class/index' });
  const goProfile = () => Taro.navigateTo({ url: '/pages/profile/index' });

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
    <View className='index-page page-with-tabbar'>
      {/* 头部 */}
      <View className='header'>
        <View className='user-info' onClick={goProfile}>
          <View className='avatar'>
            {userInfo?.avatar ? (
              <Image src={userInfo.avatar} className='avatar-img' />
            ) : (
              <Text className='avatar-text'>{userInfo?.name?.[0] || '?'}</Text>
            )}
          </View>
          <View className='user-detail'>
            <View className='user-name'>{userInfo?.name || '用户'}</View>
            <View className='user-role'>{ROLE_LABEL[role] || ''}</View>
          </View>
        </View>
        <View className='logout-btn' onClick={handleLogout}>退出</View>
      </View>

      {loading ? (
        <View className='loading'>加载中...</View>
      ) : (
        <ScrollView scrollY className='content' style={{ paddingBottom: '80px' }}>
          {/* 学生视图 */}
          {role === 'student' && (
            <>
              <View className='card today-card' onClick={goCheckin}>
                <View className='today-title'>今日运动</View>
                <View className='today-count'>
                  <Text className='count-num'>{todayCheckin.length}</Text>
                  <Text className='count-label'>次打卡</Text>
                </View>
                <View className='today-desc'>{todayCheckin.length > 0 ? '已坚持，继续加油！' : '点击去打卡'}</View>
              </View>

              {weekStats && (
                <View className='card stats-card'>
                  <View className='stats-title'>本周统计</View>
                  <View className='stats-grid' style={{ display: 'flex', flexDirection: 'row', width: '100%' }}>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.totalCalorie || 0}</Text>
                      <Text className='stats-label'>总卡路里</Text>
                    </View>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.totalDuration || 0}</Text>
                      <Text className='stats-label'>总时长(分)</Text>
                    </View>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.rank || '-'}</Text>
                      <Text className='stats-label'>班级排名</Text>
                    </View>
                  </View>
                </View>
              )}
            </>
          )}

          {/* 老师视图 */}
          {role === 'teacher' && (
            <View className='card class-card' onClick={goClass}>
              <View className='class-title'>我的班级</View>
              {classList.length > 0 ? (
                <View className='class-list'>
                  {classList.map(cls => (
                    <View className='class-item' key={cls._id}>
                      <Text className='class-name'>{cls.name}</Text>
                      <Text className='class-count'>{cls.memberCount || 0} 名学生</Text>
                    </View>
                  ))}
                </View>
              ) : (
                <View className='class-empty'>暂无班级，点击创建或加入</View>
              )}
            </View>
          )}

          {/* 家长视图 */}
          {role === 'parent' && (
            <>
              <View className='card today-card'>
                <View className='today-title'>孩子的运动</View>
                {weekStats && weekStats.summary ? (
                  <View className='stats-grid' style={{ display: 'flex', flexDirection: 'row', width: '100%', marginTop: '12px' }}>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.summary.totalCheckins || 0}</Text>
                      <Text className='stats-label'>累计打卡</Text>
                    </View>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.summary.totalCalorie || 0}</Text>
                      <Text className='stats-label'>总卡路里</Text>
                    </View>
                    <View className='stats-item' style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                      <Text className='stats-value'>{weekStats.summary.totalDuration || 0}</Text>
                      <Text className='stats-label'>总时长(分)</Text>
                    </View>
                  </View>
                ) : (
                  <View className='today-desc'>暂无数据</View>
                )}
              </View>

              <View className='card class-card' onClick={goClass}>
                <View className='class-title'>孩子的班级</View>
                {classList.length > 0 ? (
                  <View className='class-list'>
                    {classList.map(cls => (
                      <View className='class-item' key={cls._id}>
                        <Text className='class-name'>{cls.name}</Text>
                        <Text className='class-count'>{cls.memberCount || 0} 名学生</Text>
                      </View>
                    ))}
                  </View>
                ) : (
                  <View className='class-empty'>孩子暂未加入班级</View>
                )}
              </View>
            </>
          )}

          {/* 快捷入口 */}
          <View className='quick-actions' style={{ display: 'flex', flexWrap: 'wrap', gap: '10px', marginTop: '10px' }}>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={goCheckin}>
              <Text className='action-icon'>🏃</Text>
              <Text className='action-label'>运动打卡</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={goClass}>
              <Text className='action-icon'>📚</Text>
              <Text className='action-label'>班级管理</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/weekly/index' })}>
              <Text className='action-icon'>🏆</Text>
              <Text className='action-label'>周榜</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/monthly/index' })}>
              <Text className='action-icon'>⭐</Text>
              <Text className='action-label'>月度明星</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/ranking/index' })}>
              <Text className='action-icon'>📊</Text>
              <Text className='action-label'>排行榜</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/stats/index' })}>
              <Text className='action-icon'>📈</Text>
              <Text className='action-label'>数据统计</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/checkin-list/index' })}>
              <Text className='action-icon'>📋</Text>
              <Text className='action-label'>打卡记录</Text>
            </View>
            <View className='action-item' style={{ flex: 1, minWidth: 'calc(50% - 5px)' }} onClick={() => Taro.navigateTo({ url: '/pages/awards/index' })}>
              <Text className='action-icon'>🎖️</Text>
              <Text className='action-label'>我的奖项</Text>
            </View>
          </View>
        </ScrollView>
      )}
      {/* 自定义 TabBar */}
      <CustomTabBar />
    </View>
  );
}

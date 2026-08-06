// src/pages/stats/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo } from '../../utils/auth';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Stats() {
  const [type, setType] = useState('week');
  const [stats, setStats] = useState(null);
  const [dailyData, setDailyData] = useState([]);
  const [exerciseDistribution, setExerciseDistribution] = useState([]);
  const [loading, setLoading] = useState(true);
  const [maxVal, setMaxVal] = useState(100);

  const loadStats = async (t = type) => {
    setLoading(true);
    try {
      const payload = t === 'week' ? { weekOffset: 0 } : { monthOffset: 0 };
      const fn = t === 'week' ? api.getWeeklyStats : api.getMonthlyStats;
      const data = await fn(payload);
      const max = (data.dailyData || []).reduce((m, d) => Math.max(m, d.duration || 0, d.calorie || 0), 1) || 1;
      setStats(data);
      setDailyData(data.dailyData || []);
      setExerciseDistribution(data.exerciseDistribution || []);
      setMaxVal(max);
    } catch (e) {
      console.error('load stats error', e);
    } finally {
      setLoading(false);
    }
  };

  useDidShow(() => {
    const userInfo = getUserInfo();
    if (!userInfo) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    loadStats('week');
  });

  const switchType = (t) => {
    if (t === type) return;
    setType(t);
    loadStats(t);
  };

  return (
    <View className='stats-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        <View className='tab-bar'>
          <View className={`tab ${type === 'week' ? 'active' : ''}`} onClick={() => switchType('week')}>本周</View>
          <View className={`tab ${type === 'month' ? 'active' : ''}`} onClick={() => switchType('month')}>本月</View>
        </View>

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : stats ? (
          <>
            <View className='card'>
              <Text className='card-title'>运动概览</Text>
              <View className='stat-grid'>
                <View className='stat-cell'>
                  <Text className='stat-num stat-primary'>{stats.totalDuration || 0}</Text>
                  <Text className='stat-label'>总时长(分)</Text>
                </View>
                <View className='stat-cell'>
                  <Text className='stat-num stat-gold'>{stats.totalCalorie || 0}</Text>
                  <Text className='stat-label'>总千卡</Text>
                </View>
                <View className='stat-cell'>
                  <Text className='stat-num'>{stats.checkinDays || 0}</Text>
                  <Text className='stat-label'>打卡天数</Text>
                </View>
                <View className='stat-cell'>
                  <Text className='stat-num'>{stats.totalCount || 0}</Text>
                  <Text className='stat-label'>打卡次数</Text>
                </View>
              </View>
            </View>

            {dailyData.length > 0 && (
              <View className='card'>
                <Text className='card-title'>每日运动时长</Text>
                <View className='chart'>
                  {dailyData.map(d => (
                    <View className='chart-bar-wrap' key={d.date}>
                      <View className='chart-bar' style={{ height: `${Math.max((d.duration || 0) * 200 / maxVal, 4)}px` }} />
                      <Text className='chart-val'>{d.duration || 0}</Text>
                      <Text className='chart-label'>{d.label}</Text>
                    </View>
                  ))}
                </View>
              </View>
            )}

            {exerciseDistribution.length > 0 && (
              <View className='card'>
                <Text className='card-title'>运动类型分布</Text>
                {exerciseDistribution.map(item => (
                  <View className='dist-row' key={item.exerciseId}>
                    <Text className='dist-emoji'>{item.icon}</Text>
                    <View className='dist-info'>
                      <View className='dist-name-row'>
                        <Text className='dist-name'>{item.name}</Text>
                        <Text className='dist-duration'>{item.duration}分钟</Text>
                      </View>
                      <View className='dist-bar'>
                        <View className='dist-bar-inner' style={{ width: `${item.percent}%` }} />
                      </View>
                    </View>
                  </View>
                ))}
              </View>
            )}

            {dailyData.length === 0 && (
              <View className='card empty'>
                <Text className='empty-emoji'>📊</Text>
                <Text className='empty-text'>{type === 'week' ? '本周' : '本月'}还没有运动数据</Text>
              </View>
            )}
          </>
        ) : null}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

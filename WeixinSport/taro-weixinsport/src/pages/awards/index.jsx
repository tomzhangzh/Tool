// src/pages/awards/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo } from '../../utils/auth';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Awards() {
  const [awards, setAwards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('all');
  const [stats, setStats] = useState({ total: 0, gold: 0, silver: 0, bronze: 0 });

  const loadAwards = async (f = filter) => {
    setLoading(true);
    try {
      const data = await api.getMyAwards({ type: f === 'all' ? undefined : f });
      const newStats = { total: data.length, gold: 0, silver: 0, bronze: 0 };
      data.forEach(a => {
        if (a.rank === 1) newStats.gold++;
        else if (a.rank === 2) newStats.silver++;
        else if (a.rank === 3) newStats.bronze++;
      });
      setAwards(data);
      setStats(newStats);
    } catch (e) {
      console.error('load awards error', e);
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
    loadAwards('all');
  });

  const switchFilter = (f) => {
    if (f === filter) return;
    setFilter(f);
    loadAwards(f);
  };

  return (
    <View className='awards-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        <View className='card medal-summary'>
          <Text className='summary-title'>我的奖牌</Text>
          <View className='medal-row'>
            <View className='medal-cell'>
              <Text className='medal-icon medal-gold'>🥇</Text>
              <Text className='medal-num'>{stats.gold}</Text>
              <Text className='medal-label'>金牌</Text>
            </View>
            <View className='medal-cell'>
              <Text className='medal-icon medal-silver'>🥈</Text>
              <Text className='medal-num'>{stats.silver}</Text>
              <Text className='medal-label'>银牌</Text>
            </View>
            <View className='medal-cell'>
              <Text className='medal-icon medal-bronze'>🥉</Text>
              <Text className='medal-num'>{stats.bronze}</Text>
              <Text className='medal-label'>铜牌</Text>
            </View>
            <View className='medal-cell'>
              <Text className='medal-icon medal-total'>🏆</Text>
              <Text className='medal-num'>{stats.total}</Text>
              <Text className='medal-label'>总数</Text>
            </View>
          </View>
        </View>

        <View className='filter-bar'>
          <View className={`filter-tab ${filter === 'all' ? 'active' : ''}`} onClick={() => switchFilter('all')}>全部</View>
          <View className={`filter-tab ${filter === 'weekly' ? 'active' : ''}`} onClick={() => switchFilter('weekly')}>周奖项</View>
          <View className={`filter-tab ${filter === 'monthly' ? 'active' : ''}`} onClick={() => switchFilter('monthly')}>月度</View>
        </View>

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : awards.length > 0 ? (
          awards.map(award => (
            <View
              className={`card award-card ${award.rank === 1 ? 'award-gold' : award.rank === 2 ? 'award-silver' : award.rank === 3 ? 'award-bronze' : ''}`}
              key={award._id}
            >
              <View className='award-row'>
                <Text className={`award-emoji ${award.rank === 1 ? 'medal-gold' : award.rank === 2 ? 'medal-silver' : award.rank === 3 ? 'medal-bronze' : 'medal-normal'}`}>
                  {award.awardIcon}
                </Text>
                <View className='award-info'>
                  <Text className='award-title'>{award.awardName}</Text>
                  <Text className='award-period'>{award.periodLabel}</Text>
                </View>
                <View className='award-rank'>
                  <Text>{award.rank === 1 ? '🥇' : award.rank === 2 ? '🥈' : award.rank === 3 ? '🥉' : `第${award.rank}名`}</Text>
                </View>
              </View>
              {award.valueText && <Text className='award-value'>{award.valueText}</Text>}
            </View>
          ))
        ) : (
          <View className='card empty'>
            <Text className='empty-emoji'>🎖️</Text>
            <Text className='empty-text'>{filter === 'all' ? '快去运动打卡，争取第一块奖牌吧！' : '该类别下还没有奖项'}</Text>
          </View>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

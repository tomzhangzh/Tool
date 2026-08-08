// src/pages/ranking/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo } from '../../utils/auth';
import AvatarImage from '../../components/AvatarImage';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

const METRICS = [
  { id: 'calorie', name: '卡路里', unit: '千卡', icon: '🔥' },
  { id: 'duration', name: '运动时长', unit: '分钟', icon: '⏱️' },
  { id: 'frequency', name: '打卡天数', unit: '天', icon: '📅' }
];

export default function Ranking() {
  const [type, setType] = useState('week');
  const [metric, setMetric] = useState('calorie');
  const [ranking, setRanking] = useState([]);
  const [myRank, setMyRank] = useState(null);
  const [loading, setLoading] = useState(true);

  const loadRanking = async (t = type, m = metric) => {
    setLoading(true);
    try {
      const data = await api.getRanking({ type: t, metric: m });
      setRanking(data.ranking || []);
      setMyRank(data.myRank);
    } catch (e) {
      console.error('load ranking error', e);
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
    loadRanking('week', 'calorie');
  });

  const switchType = (t) => {
    if (t === type) return;
    setType(t);
    loadRanking(t, metric);
  };

  const switchMetric = (m) => {
    if (m === metric) return;
    setMetric(m);
    loadRanking(type, m);
  };

  return (
    <View className='ranking-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        <View className='tab-bar'>
          <View className={`tab ${type === 'week' ? 'active' : ''}`} onClick={() => switchType('week')}>本周</View>
          <View className={`tab ${type === 'month' ? 'active' : ''}`} onClick={() => switchType('month')}>本月</View>
        </View>

        <View className='metric-bar'>
          {METRICS.map(m => (
            <View
              key={m.id}
              className={`metric-cell ${metric === m.id ? 'active' : ''}`}
              onClick={() => switchMetric(m.id)}
            >
              <Text className='metric-emoji'>{m.icon}</Text>
              <Text className='metric-name'>{m.name}</Text>
            </View>
          ))}
        </View>

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : (
          <>
            {ranking.length > 0 ? (
              <View className='card rank-list'>
                {ranking.map((item, idx) => (
                  <View className={`rank-row ${item.isMe ? 'rank-me' : ''}`} key={item.username}>
                    <View className='rank-num'>
                      <Text>{idx === 0 ? '🥇' : idx === 1 ? '🥈' : idx === 2 ? '🥉' : (idx + 1)}</Text>
                    </View>
                    <View className='rank-avatar'>
                      {item.avatar ? <AvatarImage src={item.avatar} className='avatar' /> : <Text className='avatar-placeholder'>{item.name ? item.name[0] : '?'}</Text>}
                    </View>
                    <View className='rank-info'>
                      <Text className='rank-name'>{item.name}</Text>
                      <Text className='rank-class'>{item.className}</Text>
                    </View>
                    <Text className='rank-value'>{item.value}</Text>
                  </View>
                ))}
              </View>
            ) : (
              <View className='card empty'>
                <Text className='empty-emoji'>🏅</Text>
                <Text className='empty-text'>暂无排名数据</Text>
              </View>
            )}

            {myRank && (
              <View className='card my-rank-card'>
                <Text className='my-rank-label'>我的排名</Text>
                <View className='rank-row'>
                  <View className='rank-num'>
                    <Text>{myRank.rank === 1 ? '🥇' : myRank.rank === 2 ? '🥈' : myRank.rank === 3 ? '🥉' : myRank.rank}</Text>
                  </View>
                  <View className='rank-avatar'>
                    <Text className='avatar-placeholder'>我</Text>
                  </View>
                  <View className='rank-info'>
                    <Text className='rank-name'>我</Text>
                    <Text className='rank-class'>第 {myRank.rank} 名 / 共 {myRank.total} 人</Text>
                  </View>
                  <Text className='rank-value'>{myRank.value}</Text>
                </View>
              </View>
            )}
          </>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

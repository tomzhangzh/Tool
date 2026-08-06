// src/pages/weekly/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Button, Image } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import { WEEKLY_AWARD_TYPES } from '../../utils/constants';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Weekly() {
  const [weekOffset, setWeekOffset] = useState(0);
  const [weekLabel, setWeekLabel] = useState('');
  const [awards, setAwards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [canCalc, setCanCalc] = useState(false);
  const role = getRole();

  const loadAwards = async (offset = weekOffset) => {
    setLoading(true);
    try {
      const data = await api.getWeeklyAwards({ weekOffset: offset });
      const awardsMap = {};
      (data.awards || []).forEach(a => { awardsMap[a.awardType] = a; });
      const awardsList = WEEKLY_AWARD_TYPES.map(t => ({
        ...(awardsMap[t.id] || { awardType: t.id, winners: [] }),
        awardName: t.name,
        awardIcon: t.icon,
        desc: t.desc
      }));
      setWeekLabel(data.weekLabel);
      setAwards(awardsList);
      setCanCalc(data.canCalc);
    } catch (e) {
      console.error('load weekly awards error', e);
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
    loadAwards(0);
  });

  const changeWeek = (offset) => {
    if (offset === weekOffset) return;
    setWeekOffset(offset);
    loadAwards(offset);
  };

  const onCalc = async () => {
    Taro.showModal({
      title: '生成本周评选',
      content: '将根据本周所有同学的打卡数据生成各项评选结果，是否继续？',
      async success(res) {
        if (!res.confirm) return;
        Taro.showLoading({ title: '计算中...' });
        try {
          await api.calcWeeklyAwards({ weekOffset });
          Taro.hideLoading();
          Taro.showToast({ title: '评选已生成', icon: 'success' });
          loadAwards();
        } catch (e) {
          Taro.hideLoading();
        }
      }
    });
  };

  return (
    <View className='weekly-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        <View className='week-switch'>
          <View className={`week-tab ${weekOffset === 0 ? 'active' : ''}`} onClick={() => changeWeek(0)}>本周</View>
          <View className={`week-tab ${weekOffset === -1 ? 'active' : ''}`} onClick={() => changeWeek(-1)}>上周</View>
          <View className='week-label'>{weekLabel}</View>
        </View>

        {role === 'teacher' && canCalc && (
          <View className='calc-bar'>
            <Text className='calc-hint'>本周数据已更新，可生成评选结果</Text>
            <Button className='calc-btn' size='mini' onClick={onCalc}>生成评选</Button>
          </View>
        )}

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : (
          <>
            {awards.map(award => (
              <View className='card award-card' key={award.awardType}>
                <View className='award-header'>
                  <Text className='award-icon'>{award.awardIcon}</Text>
                  <View className='award-info'>
                    <Text className='award-title'>{award.awardName}</Text>
                    <Text className='award-desc'>{award.desc}</Text>
                  </View>
                </View>

                {award.winners && award.winners.length > 0 ? (
                  award.winners.map((winner, idx) => (
                    <View className={`winner-row ${idx === 0 ? 'winner-top' : ''}`} key={winner.openid}>
                      <View className='rank-badge'>
                        <Text>{idx === 0 ? '🥇' : idx === 1 ? '🥈' : idx === 2 ? '🥉' : (idx + 1)}</Text>
                      </View>
                      <View className='winner-avatar'>
                        {winner.avatar ? <Image src={winner.avatar} className='avatar' /> : <Text className='avatar-placeholder'>{winner.name ? winner.name[0] : '?'}</Text>}
                      </View>
                      <View className='winner-info'>
                        <Text className='winner-name'>{winner.name}</Text>
                        <Text className='winner-value'>{winner.value}</Text>
                      </View>
                      {idx === 0 && <Text className='winner-tag'>得主</Text>}
                    </View>
                  ))
                ) : (
                  <View className='empty-hint'>
                    <Text>评选结果尚未产生{weekOffset === 0 ? '，周日晚自动结算' : ''}</Text>
                  </View>
                )}
              </View>
            ))}

            <View className='card rule-card'>
              <Text className='rule-title'>评选规则</Text>
              <Text className='rule-text'>· 每周日晚自动结算，老师也可手动生成</Text>
              <Text className='rule-text'>· 每个奖项评选前3名作为获奖者</Text>
              <Text className='rule-text'>· 设立6个奖项让更多同学获得成就感</Text>
              <Text className='rule-text'>· 卡路里按体重换算更公平</Text>
            </View>
          </>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

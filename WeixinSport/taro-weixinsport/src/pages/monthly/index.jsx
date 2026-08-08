// src/pages/monthly/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Button } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import AvatarImage from '../../components/AvatarImage';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Monthly() {
  const [monthOffset, setMonthOffset] = useState(0);
  const [monthLabel, setMonthLabel] = useState('');
  const [top3, setTop3] = useState([]);
  const [specialAwards, setSpecialAwards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [canCalc, setCanCalc] = useState(false);
  const role = getRole();

  const loadStars = async (offset = monthOffset) => {
    setLoading(true);
    try {
      const data = await api.getMonthlyStars({ monthOffset: offset });
      setMonthLabel(data.monthLabel);
      setTop3(data.top3 || []);
      setSpecialAwards(data.specialAwards || []);
      setCanCalc(data.canCalc);
    } catch (e) {
      console.error('load monthly stars error', e);
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
    loadStars(0);
  });

  const changeMonth = (offset) => {
    if (offset === monthOffset) return;
    setMonthOffset(offset);
    loadStars(offset);
  };

  const onCalc = async () => {
    Taro.showModal({
      title: '生成月度明星',
      content: '将根据本月综合得分评选月度明星，是否继续？',
      success(res) {
        if (!res.confirm) return;
        Taro.showLoading({ title: '计算中...' });
        api.calcMonthlyStars({ monthOffset }).then(() => {
          Taro.hideLoading();
          Taro.showToast({ title: '已生成', icon: 'success' });
          loadStars();
        }).catch(() => Taro.hideLoading());
      }
    });
  };

  return (
    <View className='monthly-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        <View className='month-switch'>
          <View className={`month-tab ${monthOffset === 0 ? 'active' : ''}`} onClick={() => changeMonth(0)}>本月</View>
          <View className={`month-tab ${monthOffset === -1 ? 'active' : ''}`} onClick={() => changeMonth(-1)}>上月</View>
          <View className='month-label'>{monthLabel}</View>
        </View>

        {role === 'teacher' && canCalc && (
          <View className='calc-bar'>
            <Text className='calc-hint'>本月数据已更新，可生成月度明星</Text>
            <Button className='calc-btn' size='mini' onClick={onCalc}>生成</Button>
          </View>
        )}

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : (
          <>
            {top3.length > 0 && (
              <View className='card podium-card'>
                <Text className='card-title text-center'>🏆 月度运动榜单</Text>
                <View className='podium'>
                  {top3.map((item, idx) => (
                    <View className={`podium-col podium-${idx + 1}`} key={item.username}>
                      <Text className='podium-medal'>{idx === 0 ? '🥇' : idx === 1 ? '🥈' : '🥉'}</Text>
                      <View className='podium-avatar'>
                        {item.avatar ? <AvatarImage src={item.avatar} className='avatar' /> : <Text className='avatar-placeholder'>{item.name ? item.name[0] : '?'}</Text>}
                      </View>
                      <Text className='podium-name'>{item.name}</Text>
                      <Text className='podium-score'>{item.score}分</Text>
                      <Text className='podium-label'>{idx === 0 ? '冠军' : idx === 1 ? '亚军' : '季军'}</Text>
                    </View>
                  ))}
                </View>
              </View>
            )}

            {specialAwards.length > 0 && (
              <View className='card'>
                <Text className='card-title'>🌟 月度专项奖</Text>
                {specialAwards.map(item => (
                  <View className='special-row' key={item.awardType}>
                    <Text className='special-emoji'>{item.awardIcon}</Text>
                    <View className='special-info'>
                      <Text className='special-name'>{item.awardName}</Text>
                      <Text className='special-desc'>{item.desc}</Text>
                    </View>
                    <View className='special-winner'>
                      <Text className='winner-name'>{item.winner?.name || '-'}</Text>
                      <Text className='winner-reason'>{item.reason || ''}</Text>
                    </View>
                  </View>
                ))}
              </View>
            )}

            {top3.length === 0 && specialAwards.length === 0 && (
              <View className='card empty'>
                <Text className='empty-emoji'>🌙</Text>
                <Text className='empty-text'>月度明星将在月末自动评选</Text>
              </View>
            )}

            <View className='card rule-card'>
              <Text className='rule-title'>月度明星规则</Text>
              <Text className='rule-text'>· 综合得分 = 卡路里30% + 时长30% + 频率25% + 多样性15%</Text>
              <Text className='rule-text'>· 月末最后一天自动评选，老师可手动触发</Text>
              <Text className='rule-text'>· 前三名将获得月度冠军/亚军/季军称号</Text>
              <Text className='rule-text'>· 月度坚持之星需打卡满20天</Text>
              <Text className='rule-text'>· 月度进步之星对比上月进步最大</Text>
            </View>
          </>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

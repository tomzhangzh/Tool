// src/pages/checkin-list/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Button } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import { timeAgo } from '../../utils/constants';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

const FILTERS = [
  { id: 'all', name: '全部' },
  { id: 'week', name: '本周' },
  { id: 'month', name: '本月' }
];

export default function CheckinList() {
  const [list, setList] = useState([]);
  const [total, setTotal] = useState(0);
  const [filter, setFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const loadList = async (f = filter, p = page) => {
    setLoading(true);
    try {
      const data = await api.getCheckinList({ page: p, pageSize });
      setList(data.list || []);
      setTotal(data.total || 0);
    } catch (e) {
      console.error('load checkin list error', e);
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
    loadList('all', 1);
  });

  const switchFilter = (f) => {
    if (f === filter) return;
    setFilter(f);
    setPage(1);
    loadList(f, 1);
  };

  const onDelete = async (item) => {
    Taro.showModal({
      title: '删除打卡',
      content: `确定删除这条「${item.exerciseName}」的打卡记录吗？`,
      success: async (res) => {
        if (!res.confirm) return;
        try {
          await api.deleteCheckin(item._id);
          Taro.showToast({ title: '已删除', icon: 'success' });
          loadList(filter, page);
        } catch (e) {
          console.error('delete checkin error', e);
        }
      }
    });
  };

  const goCheckin = () => {
    Taro.navigateTo({ url: '/pages/checkin/index' });
  };

  return (
    <View className='checkin-list-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        {/* 筛选器 */}
        <View className='filter-bar'>
          {FILTERS.map(f => (
            <View
              key={f.id}
              className={`filter-tab ${filter === f.id ? 'active' : ''}`}
              onClick={() => switchFilter(f.id)}
            >
              {f.name}
            </View>
          ))}
          <Text className='total-label'>共{total}条</Text>
        </View>

        {loading ? (
          <View className='loading'>加载中...</View>
        ) : list.length > 0 ? (
          <View className='list'>
            {list.map(item => (
              <View className='card checkin-item' key={item._id}>
                <View className='item-left'>
                  <Text className='item-icon'>{item.exerciseIcon || '🏃'}</Text>
                </View>
                <View className='item-content'>
                  <View className='item-header'>
                    <Text className='item-name'>{item.exerciseName}</Text>
                    <Text className='item-calorie'>{item.calorie}千卡</Text>
                  </View>
                  <View className='item-detail'>
                    <Text className='item-duration'>⏱️ {item.duration}分钟</Text>
                    <Text className='item-time'>{timeAgo(item.createTime)}</Text>
                  </View>
                  {item.note && <Text className='item-note'>💬 {item.note}</Text>}
                  <Text className='item-date'>{item.dateStr}</Text>
                </View>
                <View className='item-action' onClick={() => onDelete(item)}>
                  <Text className='delete-icon'>🗑️</Text>
                </View>
              </View>
            ))}
          </View>
        ) : (
          <View className='card empty'>
            <Text className='empty-emoji'>📋</Text>
            <Text className='empty-text'>还没有打卡记录</Text>
            <Button className='go-checkin-btn' onClick={goCheckin}>去打卡</Button>
          </View>
        )}

        {list.length > 0 && total > pageSize && (
          <View className='pagination'>
            <Text className='page-info'>第 {page} / {Math.ceil(total / pageSize)} 页</Text>
          </View>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

// src/pages/checkin-list/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Button, Image } from '@tarojs/components';
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

// 辅助函数：获取图片真实 URL
const resolveImageUrl = async (image) => {
  if (!image) return '';
  // 如果已经是 http 开头，直接返回
  if (image.startsWith('http')) return image;
  // 如果是 fileID，获取临时链接
  try {
    const data = await api.getImageUrl(image);
    return data.url || '';
  } catch (e) {
    console.error('getImageUrl error', e);
    return '';
  }
};

export default function CheckinList() {
  const [list, setList] = useState([]);
  const [total, setTotal] = useState(0);
  const [filter, setFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [previewImage, setPreviewImage] = useState('');
  const pageSize = 20;

  const loadList = async (f = filter, p = page) => {
    setLoading(true);
    try {
      const userInfo = getUserInfo();
      const data = await api.getCheckinList({ 
        page: p, 
        pageSize,
        username: userInfo?.username // 传递 username 用于容错
      });
      
      // 处理图片 URL：将 fileID 转换为可访问的 URL
      const items = data.list || [];
      const processedList = await Promise.all(
        items.map(async (item) => {
          const itemImage = item.image || '';
          let displayImage = '';
          if (itemImage) {
            displayImage = await resolveImageUrl(itemImage);
          }
          return { ...item, displayImage };
        })
      );
      
      setList(processedList);
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

  const previewImg = (url) => {
    if (!url) return;
    setPreviewImage(url);
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
                  {item.displayImage && (
                    <View className='item-image' onClick={() => previewImg(item.displayImage)}>
                      <Image src={item.displayImage} className='item-img' mode='aspectFit' />
                    </View>
                  )}
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
      
      {/* 图片预览弹窗 */}
      {previewImage && (
        <View className='img-preview-mask' style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0, 0, 0, 0.95)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', zIndex: 9999 }} onClick={() => setPreviewImage('')}>
          <Image src={previewImage} className='img-preview-big' style={{ width: '100%', height: '100%' }} mode='aspectFit' />
          <Text className='img-preview-close' style={{ color: '#fff', fontSize: '14px', marginTop: '10px', position: 'absolute', bottom: '40px' }}>点击关闭</Text>
        </View>
      )}
      
      <CustomTabBar />
    </View>
  );
}

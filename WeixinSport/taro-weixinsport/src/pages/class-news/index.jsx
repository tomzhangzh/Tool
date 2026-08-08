// src/pages/class-news/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Image } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo } from '../../utils/auth';
import { timeAgo, shortDateTime } from '../../utils/constants';
import { resolveFileURL } from '../../utils/cloud';
import CustomTabBar from '../../components/CustomTabBar';
import LikeDetail from '../../components/LikeDetail';

export default function ClassNews() {
  const [classes, setClasses] = useState([]);
  const [selectedClass, setSelectedClass] = useState(null);
  const [feedList, setFeedList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [previewImage, setPreviewImage] = useState('');
  const [timeFilter, setTimeFilter] = useState('today');
  const [likedMap, setLikedMap] = useState({});
  const [likeDetailTarget, setLikeDetailTarget] = useState(null);

  // 根据时间周期过滤列表
  const getFilteredList = () => {
    const now = new Date();
    const nowStart = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
    const dayMs = 24 * 60 * 60 * 1000;
    let startTime = 0;

    if (timeFilter === 'today') {
      startTime = nowStart;
    } else if (timeFilter === 'week') {
      // 本周一
      const dayOfWeek = now.getDay() || 7;
      startTime = nowStart - (dayOfWeek - 1) * dayMs;
    } else if (timeFilter === 'month') {
      startTime = new Date(now.getFullYear(), now.getMonth(), 1).getTime();
    }

    return feedList.filter(item => (item.createTime || 0) >= startTime);
  };

  const loadClasses = async () => {
    setLoading(true);
    try {
      const userInfo = getUserInfo();
      const list = await api.getMyClasses(userInfo?.username);
      console.log('[class-news] 班级列表:', list);
      setClasses(list || []);
      if (list && list.length > 0) {
        setSelectedClass(list[0]);
        loadFeed(list[0]._id);
      } else {
        setLoading(false);
      }
    } catch (e) {
      console.error('load classes error', e);
      setLoading(false);
    }
  };

  const loadFeed = async (classId) => {
    setLoading(true);
    try {
      const userInfo = getUserInfo();
      console.log('[class-news] 加载班级动态, classId:', classId);
      const data = await api.getClassFeed({ 
        classId, 
        page: 1, 
        pageSize: 50,
        username: userInfo?.username 
      });
      console.log('[class-news] 动态数据:', data);
      
      const items = data.list || [];
      const processedList = await Promise.all(
        items.map(async (item) => {
          const itemImage = item.image || '';
          let displayImage = '';
          if (itemImage) {
            displayImage = await resolveFileURL(itemImage);
          }
          // 转换头像 URL
          let displayAvatar = '';
          if (item.avatar) {
            displayAvatar = await resolveFileURL(item.avatar);
          }
          return { ...item, displayImage, displayAvatar };
        })
      );
      
      setFeedList(processedList);

      // 批量获取点赞状态
      if (processedList.length > 0 && userInfo?.username) {
        try {
          const targetIds = processedList.map(item => item._id);
          const likes = await api.batchCheckLikes(targetIds);
          setLikedMap(likes || {});
        } catch (e) {
          console.error('batch check likes error', e);
        }
      }
    } catch (e) {
      console.error('load feed error', e);
    } finally {
      setLoading(false);
    }
  };

  const handleLike = async (item) => {
    try {
      const result = await api.toggleLike(item._id);
      setLikedMap(prev => ({ ...prev, [item._id]: result.liked }));
      // 更新列表中的点赞数
      setFeedList(prev => prev.map(i => 
        i._id === item._id 
          ? { ...i, likeCount: result.likeCount }
          : i
      ));
      Taro.vibrateShort && Taro.vibrateShort({ type: 'light' });
    } catch (e) {
      console.error('toggle like error', e);
    }
  };

  const openLikeDetail = (item) => {
    if (item.likeCount > 0) {
      setLikeDetailTarget(item._id);
    }
  };

  const closeLikeDetail = () => {
    setLikeDetailTarget(null);
  };

  useDidShow(() => {
    console.log('[class-news] useDidShow 触发');
    const userInfo = getUserInfo();
    if (!userInfo) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    loadClasses();
  });

  const switchClass = (cls) => {
    setSelectedClass(cls);
    loadFeed(cls._id);
  };

  const previewImg = (url) => {
    if (!url) return;
    setPreviewImage(url);
  };

  return (
    <View className='class-news-page page-with-tabbar' style={{ minHeight: '100vh', background: '#f7f8fa' }}>
      <ScrollView scrollY className='content' style={{ padding: '12px', paddingBottom: '100px' }}>
        <View className='page-header' style={{ 
          fontSize: '18px', 
          fontWeight: 600, 
          padding: '14px 16px', 
          background: 'linear-gradient(135deg, #4A90E2 0%, #5AA8F5 100%)', 
          color: '#fff', 
          textAlign: 'center', 
          borderRadius: '8px',
          marginBottom: '12px'
        }}>📣 班级动态</View>
        
        {classes.length > 0 && (
          <View className='class-tabs' style={{ marginBottom: '12px' }}>
            <ScrollView scrollX className='class-tabs-scroll' style={{ whiteSpace: 'nowrap' }}>
              {classes.map(cls => (
                <View
                  key={cls._id}
                  className={`class-tab ${selectedClass?._id === cls._id ? 'active' : ''}`}
                  style={{
                    display: 'inline-block',
                    padding: '6px 14px',
                    marginRight: '8px',
                    background: selectedClass?._id === cls._id ? '#4A90E2' : '#fff',
                    borderRadius: '20px',
                    fontSize: '13px',
                    color: selectedClass?._id === cls._id ? '#fff' : '#666'
                  }}
                  onClick={() => switchClass(cls)}
                >
                  {cls.name}
                </View>
              ))}
            </ScrollView>
          </View>
        )}

        {/* 时间周期筛选 */}
        <View className='time-filter' style={{ display: 'flex', background: '#fff', borderRadius: '8px', marginBottom: '12px', overflow: 'hidden', boxShadow: '0 1px 6px rgba(0,0,0,0.04)' }}>
          {[
            { key: 'today', label: '今日' },
            { key: 'week', label: '本周' },
            { key: 'month', label: '本月' }
          ].map(item => (
            <View
              key={item.key}
              className={`time-filter-item ${timeFilter === item.key ? 'active' : ''}`}
              style={{
                flex: 1,
                textAlign: 'center',
                padding: '10px 0',
                fontSize: '14px',
                color: timeFilter === item.key ? '#fff' : '#666',
                background: timeFilter === item.key ? '#4A90E2' : '#fff',
                cursor: 'pointer',
                transition: 'all 0.2s'
              }}
              onClick={() => setTimeFilter(item.key)}
            >
              {item.label}
            </View>
          ))}
        </View>

        {loading ? (
          <View className='loading' style={{ textAlign: 'center', padding: '40px', color: '#999', fontSize: '14px' }}>加载中...</View>
        ) : classes.length === 0 ? (
          <View className='empty-card' style={{ background: '#fff', borderRadius: '12px', padding: '40px 20px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Text className='empty-emoji' style={{ fontSize: '40px', marginBottom: '12px' }}>📚</Text>
            <Text className='empty-text' style={{ fontSize: '15px', color: '#333', marginBottom: '6px' }}>暂未加入班级</Text>
            <Text className='empty-desc' style={{ fontSize: '13px', color: '#999', marginBottom: '20px' }}>加入班级后可查看同学的打卡动态</Text>
            <View 
              onClick={() => Taro.navigateTo({ url: '/pages/class/index' })}
              style={{ background: '#4A90E2', color: '#fff', padding: '10px 30px', borderRadius: '20px', fontSize: '14px', fontWeight: 500 }}
            >➕ 创建/加入班级</View>
          </View>
        ) : feedList.length === 0 ? (
          <View className='empty-card' style={{ background: '#fff', borderRadius: '12px', padding: '40px 20px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Text className='empty-emoji' style={{ fontSize: '40px', marginBottom: '12px' }}>🏃</Text>
            <Text className='empty-text' style={{ fontSize: '15px', color: '#333', marginBottom: '6px' }}>暂无打卡动态</Text>
            <Text className='empty-desc' style={{ fontSize: '13px', color: '#999' }}>成为第一个打卡的人吧！</Text>
          </View>
        ) : getFilteredList().length === 0 ? (
          <View className='empty-card' style={{ background: '#fff', borderRadius: '12px', padding: '40px 20px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Text className='empty-emoji' style={{ fontSize: '40px', marginBottom: '12px' }}>📭</Text>
            <Text className='empty-text' style={{ fontSize: '15px', color: '#333', marginBottom: '6px' }}>当前时间周期暂无动态</Text>
            <Text className='empty-desc' style={{ fontSize: '13px', color: '#999' }}>切换到其他周期查看更多</Text>
          </View>
        ) : (
          <View className='feed-list'>
            {getFilteredList().map(item => {
              const isLiked = likedMap[item._id];
              const likeCount = item.likeCount || 0;
              return (
                <View className='feed-card' key={item._id} style={{ background: '#fff', borderRadius: '12px', padding: '14px', marginBottom: '12px', boxShadow: '0 1px 6px rgba(0, 0, 0, 0.04)' }}>
                  <View className='feed-header' style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '10px' }}>
                    <View className='feed-user' style={{ display: 'flex', alignItems: 'center', flex: 1 }}>
                      {item.displayAvatar ? (
                        <Image src={item.displayAvatar} className='feed-avatar' style={{ width: '36px', height: '36px', borderRadius: '50%' }} />
                      ) : (
                        <View className='feed-avatar-placeholder' style={{ width: '36px', height: '36px', borderRadius: '50%', background: '#4A90E2', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '16px', marginRight: '10px' }}>
                          <Text>{item.userName?.[0] || '?'}</Text>
                        </View>
                      )}
                      <View className='feed-user-info' style={{ marginLeft: '10px', flex: 1 }}>
                        <Text className='feed-username' style={{ fontSize: '14px', fontWeight: 500, color: '#333', display: 'block' }}>{item.userName}</Text>
                        <Text className='feed-time' style={{ fontSize: '12px', color: '#999', display: 'block', marginTop: '2px' }}>{timeAgo(item.createTime)}</Text>
                      </View>
                    </View>
                    <Text className='feed-calorie' style={{ fontSize: '13px', color: '#ff6b35', fontWeight: 500, flexShrink: 0 }}>🔥 {item.calorie}千卡</Text>
                  </View>
                  
                  <View className='feed-content' style={{ paddingTop: '8px', borderTop: '1px solid #f5f5f5' }}>
                    <View className='feed-exercise' style={{ display: 'flex', alignItems: 'center', marginBottom: '6px', flexWrap: 'wrap' }}>
                      <Text className='feed-exercise-icon' style={{ fontSize: '20px', marginRight: '6px' }}>{item.exerciseIcon || '🏃'}</Text>
                      <Text className='feed-exercise-name' style={{ fontSize: '15px', fontWeight: 500, color: '#333', marginRight: '8px' }}>{item.exerciseName}</Text>
                      <Text className='feed-duration' style={{ fontSize: '13px', color: '#666' }}>{item.duration}分钟</Text>
                    </View>
                    
                    {item.note && <Text className='feed-note' style={{ display: 'block', fontSize: '13px', color: '#666', marginBottom: '10px', lineHeight: 1.5 }}>"{item.note}"</Text>}
                    
                    {item.displayImage && (
                      <View className='feed-image' style={{ borderRadius: '8px', overflow: 'hidden', marginTop: '8px', background: '#f5f5f5', display: 'flex', justifyContent: 'center' }} onClick={() => previewImg(item.displayImage)}>
                        <Image src={item.displayImage} className='feed-img' style={{ maxWidth: '100%', maxHeight: '300px', display: 'block' }} mode='aspectFit' />
                      </View>
                    )}
                  </View>
                  
                  <View className='feed-footer' style={{ marginTop: '10px', paddingTop: '8px', borderTop: '1px solid #f5f5f5', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Text className='feed-date' style={{ fontSize: '12px', color: '#bbb' }}>{shortDateTime(item.createTime)}</Text>
                    <View className='feed-footer-actions' style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      {likeCount > 0 && (
                        <View className='feed-like-count' onClick={() => openLikeDetail(item)} style={{ display: 'flex', alignItems: 'center', padding: '4px 8px', borderRadius: '16px', cursor: 'pointer', background: '#fff5f5' }}>
                          <Text style={{ fontSize: '14px', marginRight: '4px' }}>❤️</Text>
                          <Text style={{ fontSize: '13px', color: '#ff6b6b', fontWeight: 500 }}>{likeCount}</Text>
                        </View>
                      )}
                      <View className={`feed-like ${isLiked ? 'liked' : ''}`} onClick={() => handleLike(item)} style={{ display: 'flex', alignItems: 'center', padding: '4px 10px', borderRadius: '16px', cursor: 'pointer', transition: 'all 0.2s' }}>
                        <Text style={{ fontSize: '18px' }}>{isLiked ? '❤️' : '🤍'}</Text>
                      </View>
                    </View>
                  </View>
                </View>
              );
            })}
          </View>
        )}
      </ScrollView>
      
      {previewImage && (
        <View className='img-preview-mask' style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0, 0, 0, 0.95)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', zIndex: 9999 }} onClick={() => setPreviewImage('')}>
          <Image src={previewImage} className='img-preview-big' style={{ width: '100%', height: '100%' }} mode='aspectFit' />
          <Text className='img-preview-close' style={{ color: '#fff', fontSize: '14px', marginTop: '10px', position: 'absolute', bottom: '40px' }}>点击关闭</Text>
        </View>
      )}

      {/* 点赞详情弹窗 */}
      <LikeDetail 
        visible={!!likeDetailTarget} 
        targetId={likeDetailTarget} 
        onClose={closeLikeDetail} 
      />
      
      <CustomTabBar />
    </View>
  );
}

// src/components/LikeDetail/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text, ScrollView, Image } from '@tarojs/components';
import Taro from '@tarojs/taro';
import api from '../../utils/api';
import { shortDateTime } from '../../utils/constants';
import { resolveFileURLs } from '../../utils/cloud';
import './index.scss';

/**
 * 点赞详情弹窗组件
 * 显示点赞用户列表
 */
export default function LikeDetail({ visible, targetId, targetType = 'checkin', onClose }) {
  const [likes, setLikes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);

  useEffect(() => {
    if (visible && targetId) {
      loadLikes();
    }
  }, [visible, targetId]);

  const loadLikes = async () => {
    if (!targetId) return;
    setLoading(true);
    try {
      const data = await api.getLikeList(targetId, targetType, { page: 1, pageSize: 50 });
      const list = data.list || [];
      
      // 收集所有 fileID，批量解析以减少云函数调用
      const fileIDs = [];
      list.forEach(like => {
        if (like.avatar) fileIDs.push(like.avatar);
      });
      
      let urlMap = new Map();
      if (fileIDs.length > 0) {
        urlMap = await resolveFileURLs(fileIDs);
      }
      
      const processedList = list.map(like => ({
        ...like,
        displayAvatar: like.avatar ? urlMap.get(like.avatar) || '' : ''
      }));
      
      setLikes(processedList);
      setTotal(data.total || 0);
    } catch (e) {
      console.error('load likes error', e);
    } finally {
      setLoading(false);
    }
  };

  if (!visible) return null;

  return (
    <View className='like-detail-mask' onClick={onClose}>
      <View className='like-modal' onClick={(e) => e.stopPropagation()}>
        <View className='like-modal-header'>
          <Text className='like-modal-title'>
            ❤️ {total > 0 ? `${total}人点赞` : '暂无点赞'}
          </Text>
          <View className='like-modal-close' onClick={onClose}>
            <Text>✕</Text>
          </View>
        </View>

        <ScrollView scrollY className='like-list'>
          {loading ? (
            <View className='like-loading'>加载中...</View>
          ) : likes.length > 0 ? (
            likes.map(like => (
              <View className='like-item' key={like._id}>
                {like.displayAvatar ? (
                  <Image src={like.displayAvatar} className='like-avatar' />
                ) : (
                  <View className='like-avatar-placeholder'>
                    <Text>{like.userName?.[0] || '?'}</Text>
                  </View>
                )}
                <View className='like-info'>
                  <Text className='like-username'>{like.userName || '匿名用户'}</Text>
                  <Text className='like-time'>{shortDateTime(like.createTime)}</Text>
                </View>
              </View>
            ))
          ) : (
            <View className='like-empty'>
              <Text className='like-empty-icon'>👍</Text>
              <Text className='like-empty-text'>还没有点赞，快去点赞吧！</Text>
            </View>
          )}
        </ScrollView>
      </View>
    </View>
  );
}

// src/pages/class-detail/index.jsx
import React, { useState } from 'react';
import { View, Text, ScrollView, Image } from '@tarojs/components';
import Taro, { useDidShow, useRouter } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo } from '../../utils/auth';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function ClassDetail() {
  const router = useRouter();
  const [classId, setClassId] = useState('');
  const [info, setInfo] = useState(null);
  const [members, setMembers] = useState([]);
  const [teachers, setTeachers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [isCreator, setIsCreator] = useState(false);

  useDidShow(() => {
    // 兼容 H5 和小程序环境获取参数
    const id = router.params?.id || '';
    console.log('[class-detail] router.params:', router.params);
    console.log('[class-detail] id:', id);
    if (id) {
      setClassId(id);
      loadDetail(id);
    } else {
      setLoading(false);
    }
  });

  const loadDetail = async (id) => {
    setLoading(true);
    try {
      const [detail, memberList, teacherList] = await Promise.all([
        api.getClassDetail(id),
        api.getClassMembers(id),
        api.getClassTeachers(id)
      ]);
      const openid = getUserInfo()?.openid;
      setInfo(detail);
      setMembers(memberList);
      setTeachers(teacherList);
      setIsCreator(detail?.teacherOpenid === openid);
    } catch (e) {
      console.error('load class detail error', e);
    } finally {
      setLoading(false);
    }
  };

  const copyCode = () => {
    if (info?.code) {
      Taro.setClipboardData({
        data: info.code,
        success: () => Taro.showToast({ title: '已复制邀请码', icon: 'success' })
      });
    }
  };

  const removeTeacher = async (targetOpenid, targetName) => {
    Taro.showModal({
      title: '移除老师',
      content: `确定将「${targetName}」移出班级管理？`,
      confirmColor: '#e64340',
      success: async (res) => {
        if (!res.confirm) return;
        try {
          await api.removeTeacher(classId, targetOpenid);
          Taro.showToast({ title: '已移除', icon: 'success' });
          loadDetail(classId);
        } catch (e) {
          console.error('remove teacher error', e);
        }
      }
    });
  };

  return (
    <View className='class-detail-page page-with-tabbar'>
      {loading ? (
        <View className='loading'>加载中...</View>
      ) : info ? (
        <ScrollView scrollY className='content'>
          {/* 班级信息卡片 */}
          <View className='card class-info' onClick={copyCode}>
            <View className='class-name'>{info.name}</View>
            <View className='class-meta'>
              <Text>{info.memberCount || 0} 名学生</Text>
              <Text className='dot'>·</Text>
              <Text>{teachers.length} 位老师</Text>
            </View>
            <View className='code-display'>
              <Text className='code-text'>邀请码：{info.code}</Text>
              <Text className='copy-hint'>点击复制</Text>
            </View>
          </View>

          {/* 管理入口 */}
          {info.isTeacher && (
            <View className='action-row'>
              <View className='action-item' onClick={() => Taro.navigateTo({ url: `/pages/stats/index?classId=${classId}` })}>
                <Text className='action-icon'>📊</Text>
                <Text className='action-label'>班级统计</Text>
              </View>
              <View className='action-item' onClick={() => Taro.navigateTo({ url: `/pages/ranking/index?classId=${classId}` })}>
                <Text className='action-icon'>🏆</Text>
                <Text className='action-label'>班级排名</Text>
              </View>
            </View>
          )}

          {/* 管理老师列表 */}
          {teachers.length > 0 && (
            <View className='card'>
              <View className='card-title'>{info.isTeacher ? '管理老师' : '班级老师'}（{teachers.length}）</View>
              <View className='teacher-list'>
                {teachers.map(teacher => (
                  <View className='teacher-item' key={teacher.openid}>
                    <View className='avatar'>
                      {teacher.avatar ? (
                        <Image src={teacher.avatar} className='avatar-img' />
                      ) : (
                        <Text className='avatar-text'>{teacher.name?.[0] || '?'}</Text>
                      )}
                    </View>
                    <View className='teacher-info'>
                      <Text className='teacher-name'>{teacher.name || '未命名'}</Text>
                      <Text className='teacher-role'>{teacher.role === 'creator' ? '创建者' : '共管老师'}</Text>
                    </View>
                    {isCreator && teacher.role !== 'creator' && (
                      <View className='remove-btn' onClick={() => removeTeacher(teacher.openid, teacher.name)}>
                        移除
                      </View>
                    )}
                  </View>
                ))}
              </View>
            </View>
          )}

          {/* 学生列表 */}
          {members.length > 0 && (
            <View className='card'>
              <View className='card-title'>学生列表（{members.length}）</View>
              <View className='member-list'>
                {members.map(member => (
                  <View className='member-item' key={member._id}>
                    <View className='rank-num'>#{members.indexOf(member) + 1}</View>
                    <View className='avatar small'>
                      {member.avatar ? (
                        <Image src={member.avatar} className='avatar-img' />
                      ) : (
                        <Text className='avatar-text'>{member.name?.[0] || '?'}</Text>
                      )}
                    </View>
                    <View className='member-info'>
                      <Text className='member-name'>{member.name}</Text>
                      <Text className='member-stat'>{member.totalCalorie || 0} 千卡 · {member.totalCheckins || 0} 次</Text>
                    </View>
                  </View>
                ))}
              </View>
            </View>
          )}
        </ScrollView>
      ) : (
        <View className='empty'>
          <Text>班级不存在</Text>
        </View>
      )}
      <CustomTabBar />
    </View>
  );
}

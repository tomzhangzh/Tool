// src/pages/checkin/index.jsx
import React, { useState } from 'react';
import { View, Text, Input, ScrollView, Button, Slider } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import { EXERCISE_TYPES, calcCalorie } from '../../utils/constants';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Checkin() {
  const [selectedExercise, setSelectedExercise] = useState(null);
  const [duration, setDuration] = useState(30);
  const [note, setNote] = useState('');
  const [weight, setWeight] = useState(30);
  const [calorie, setCalorie] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [todayList, setTodayList] = useState([]);

  useDidShow(() => {
    const userInfo = getUserInfo();
    const currentRole = getRole();
    if (!userInfo) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    if (currentRole !== 'student') {
      Taro.showToast({ title: '仅学生可打卡', icon: 'none' });
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 1000);
      return;
    }
    setWeight(userInfo.weight || 30);
    loadToday();
  });

  const loadToday = async () => {
    try {
      const list = await api.getTodayCheckin();
      setTodayList(list);
    } catch (e) {
      console.error('load today error', e);
    }
  };

  const onSelectExercise = (exercise) => {
    setSelectedExercise(exercise);
    if (duration > 0) {
      setCalorie(calcCalorie(exercise.met, duration, weight));
    }
  };

  const onDurationChange = (e) => {
    const d = Number(e.detail.value) || 0;
    setDuration(d);
    if (selectedExercise) {
      setCalorie(calcCalorie(selectedExercise.met, d, weight));
    }
  };

  const handleSubmit = async () => {
    if (!selectedExercise) {
      Taro.showToast({ title: '请选择运动项目', icon: 'none' });
      return;
    }
    if (!duration || duration <= 0) {
      Taro.showToast({ title: '请填写运动时长', icon: 'none' });
      return;
    }

    setSubmitting(true);
    try {
      await api.submitCheckin({
        exerciseId: selectedExercise.id,
        exerciseName: selectedExercise.name,
        exerciseIcon: selectedExercise.icon,
        met: selectedExercise.met,
        duration,
        note
      });
      Taro.showToast({ title: '打卡成功！', icon: 'success' });
      setSelectedExercise(null);
      setDuration(30);
      setNote('');
      setCalorie(0);
      loadToday();
    } catch (e) {
      console.error('submit error', e);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View className='checkin-page page-with-tabbar'>
      <ScrollView scrollY className='content'>
        {/* 运动项目选择 */}
        <View className='card'>
          <View className='section-title'>选择运动项目</View>
          <View className='exercise-grid'>
            {EXERCISE_TYPES.map(ex => (
              <View 
                key={ex.id} 
                className={`exercise-item ${selectedExercise?.id === ex.id ? 'active' : ''}`}
                onClick={() => onSelectExercise(ex)}
              >
                <Text className='exercise-icon'>{ex.icon}</Text>
                <Text className='exercise-name'>{ex.name}</Text>
              </View>
            ))}
          </View>
        </View>

        {/* 运动详情 */}
        {selectedExercise && (
          <View className='card'>
            <View className='section-title'>运动详情</View>
            
            <View className='form-row'>
              <Text className='form-label'>运动时长</Text>
              <View className='duration-input'>
                <Slider 
                  className='duration-slider'
                  min={5} 
                  max={240} 
                  step={5} 
                  value={duration}
                  activeColor='#4A90E2'
                  backgroundColor='#e0e0e0'
                  blockSize={24}
                  onChange={onDurationChange}
                />
                <Text className='text-muted'>{duration} 分钟</Text>
              </View>
            </View>

            <View className='form-row'>
              <Text className='form-label'>预计消耗</Text>
              <Text className='calorie-text'>🔥 {calorie} 千卡</Text>
            </View>

            <View className='form-row'>
              <Text className='form-label'>备注</Text>
              <Input 
                className='note-input' 
                placeholder='今天的感觉如何？（选填）'
                value={note}
                onInput={(e) => setNote(e.detail.value)}
              />
            </View>

            <Button 
              className='submit-btn'
              onClick={handleSubmit}
              loading={submitting}
              disabled={submitting}
            >
              完成打卡
            </Button>
          </View>
        )}

        {/* 今日打卡记录 */}
        {todayList.length > 0 && (
          <View className='card'>
            <View className='section-title'>
              <Text>今日已打卡</Text>
              <Text className='text-muted text-small' onClick={() => Taro.navigateTo({ url: '/pages/checkin-list/index' })}>查看全部 ›</Text>
            </View>
            <View className='today-list'>
              {todayList.map(item => (
                <View className='today-item' key={item._id}>
                  <Text className='item-icon'>{item.exerciseIcon || '🏃'}</Text>
                  <View className='item-info'>
                    <Text className='item-name'>{item.exerciseName}</Text>
                    <Text className='item-detail'>{item.duration}分钟 · {item.calorie}千卡</Text>
                  </View>
                </View>
              ))}
            </View>
          </View>
        )}
      </ScrollView>
      <CustomTabBar />
    </View>
  );
}

// src/pages/checkin/index.jsx
import React, { useState, useRef } from 'react';
import { View, Text, Input, ScrollView, Button, Slider, Image } from '@tarojs/components';
import Taro, { useDidShow } from '@tarojs/taro';
import api from '../../utils/api';
import { getUserInfo, getRole } from '../../utils/auth';
import { EXERCISE_TYPES, calcCalorie } from '../../utils/constants';
import { callFunction } from '../../utils/cloud';
import { compressImageH5, blobToBase64 } from '../../utils/compress';
import CustomTabBar from '../../components/CustomTabBar';
import './index.scss';

export default function Checkin() {
  const [selectedExercise, setSelectedExercise] = useState(null);
  const [duration, setDuration] = useState(30);
  const [note, setNote] = useState('');
  const [weight, setWeight] = useState(60);
  const [calorie, setCalorie] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [todayList, setTodayList] = useState([]);
  const [imagePreview, setImagePreview] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef(null);

  useDidShow(() => {
    const userInfo = getUserInfo();
    const currentRole = getRole();
    if (!userInfo) {
      Taro.redirectTo({ url: '/pages/login/index' });
      return;
    }
    // 兼容处理：优先从 getRole() 获取，回退到 userInfo.role
    const role = currentRole || userInfo.role;
    console.log('[checkin] 角色检查:', { currentRole, userRole: userInfo.role, finalRole: role });
    if (role !== 'student') {
      Taro.showToast({ title: '仅学生可打卡', icon: 'none' });
      setTimeout(() => {
        Taro.switchTab({ url: '/pages/index/index' });
      }, 1000);
      return;
    }
    setWeight(userInfo.weight || 60);
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

  // H5 环境：选择图片
  const handleChooseImageH5 = () => {
    if (uploading) return;
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  // H5 环境：处理图片选择、压缩和上传
  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // 清空 input 值，允许重复选择同一文件
    e.target.value = '';

    try {
      setUploading(true);
      Taro.showLoading({ title: '压缩中...', mask: true });

      // 压缩图片到 1MB 以内
      const compressedBlob = await compressImageH5(file, 1024 * 1024);
      
      // 转换为 base64 用于预览和云函数传输
      const base64 = await blobToBase64(compressedBlob);
      
      // 显示预览
      setImagePreview(base64);

      // 生成云端路径
      const ext = file.type.includes('png') ? 'png' : 'jpg';
      const ts = Date.now();
      const cloudPath = `checkin/${ts}.${ext}`;

      Taro.showLoading({ title: '上传中...', mask: true });

      // 通过 login 云函数的 uploadAvatar action 上传（复用头像上传接口）
      const result = await callFunction('login', {
        action: 'uploadAvatar',
        fileContent: base64,
        cloudPath,
      });

      if (result.result?.code !== 0) {
        throw new Error(result.result?.message || '上传失败');
      }

      const fileID = result.result.data.fileID;
      const url = result.result.data.url;
      // 保存 fileID（永久有效）和 url（临时）
      console.log('[upload] fileID:', fileID, 'url:', url);
      setImageUrl(fileID);  // 保存 fileID，用于持久存储
      setImagePreview(url);  // 显示临时链接用于预览
      
      Taro.hideLoading();
      Taro.showToast({ title: '图片上传成功', icon: 'success' });
    } catch (err) {
      console.error('图片上传失败', err);
      Taro.hideLoading();
      Taro.showToast({ title: err.message || '上传失败，请重试', icon: 'none' });
      // 上传失败时清除预览
      setImagePreview('');
      setImageUrl('');
    } finally {
      setUploading(false);
    }
  };

  // 小程序环境：选择图片
  const handleChooseImageMini = async () => {
    if (uploading) return;
    
    try {
      const res = await Taro.chooseImage({
        count: 1,
        sizeType: ['compressed'], // 压缩图
        sourceType: ['album', 'camera']
      });

      const tempFilePath = res.tempFilePaths?.[0];
      if (!tempFilePath) return;

      setUploading(true);
      Taro.showLoading({ title: '上传中...', mask: true });

      // 生成云端路径
      const ext = tempFilePath.includes('.png') ? 'png' : 'jpg';
      const ts = Date.now();
      const cloudPath = `checkin/${ts}.${ext}`;

      // 小程序环境：直接上传到云存储
      const uploadResult = await new Promise((resolve, reject) => {
        Taro.cloud.uploadFile({
          cloudPath,
          filePath: tempFilePath,
          success: (res) => resolve({ url: res.fileID }),
          fail: reject
        });
      });

      setImageUrl(uploadResult.url);
      setImagePreview(tempFilePath); // 小程序使用临时路径预览
      
      Taro.hideLoading();
      Taro.showToast({ title: '图片上传成功', icon: 'success' });
    } catch (err) {
      console.error('图片上传失败', err);
      Taro.hideLoading();
      Taro.showToast({ title: '上传失败，请重试', icon: 'none' });
    } finally {
      setUploading(false);
    }
  };

  // 删除已上传的图片
  const handleRemoveImage = () => {
    setImagePreview('');
    setImageUrl('');
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
      const userInfo = getUserInfo();
      console.log('[submit] imageUrl:', imageUrl, 'imagePreview:', imagePreview);
      await api.submitCheckin({
        exerciseId: selectedExercise.id,
        exerciseName: selectedExercise.name,
        exerciseIcon: selectedExercise.icon,
        met: selectedExercise.met,
        duration,
        note,
        image: imageUrl, // 上传的图片 fileID
        username: userInfo?.username // 传递 username 用于容错查找用户
      });
      console.log('[submit] 打卡成功');
      Taro.showToast({ title: '打卡成功！', icon: 'success' });
      setSelectedExercise(null);
      setDuration(30);
      setNote('');
      setCalorie(0);
      setImagePreview('');
      setImageUrl('');
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

            {/* 图片上传 */}
            <View className='form-row image-upload-row'>
              <Text className='form-label'>运动图片</Text>
              <View className='image-upload-area'>
                {imagePreview ? (
                  <View className='image-preview-wrapper'>
                    <Image 
                      src={imagePreview} 
                      className='image-preview' 
                      mode='aspectFit'
                    />
                    <View className='image-remove-btn' onClick={handleRemoveImage}>×</View>
                  </View>
                ) : (
                  <View 
                    className={`image-upload-btn ${uploading ? 'uploading' : ''}`}
                    onClick={process.env.TARO_ENV === 'h5' ? handleChooseImageH5 : handleChooseImageMini}
                  >
                    <Text className='upload-icon'>📷</Text>
                    <Text className='upload-text'>{uploading ? '上传中...' : '添加图片'}</Text>
                    <Text className='upload-hint'>（最多1张，自动压缩到1M以内）</Text>
                  </View>
                )}
                
                {/* H5 环境隐藏的文件选择器 */}
                {process.env.TARO_ENV === 'h5' && (
                  <input
                    ref={fileInputRef}
                    type='file'
                    accept='image/*'
                    style={{ display: 'none' }}
                    onChange={handleFileChange}
                  />
                )}
              </View>
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
